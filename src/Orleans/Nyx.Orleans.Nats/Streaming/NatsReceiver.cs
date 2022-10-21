using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.JetStream;
using Newtonsoft.Json;
using Nyx.Orleans.Serialization;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

internal class NatsReceiver : IQueueAdapterReceiver
{
    private readonly string _providerName;
    private readonly QueueId _queueId;
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _natsStreamConsumer;
    private readonly string _natsStreamName;
    private readonly string _natsSubjectPattern;
    private IConnection? _connection = null;
    private IJetStream? _jetStreamContext;
    private IJetStreamPullSubscription? _subscription;
    private readonly NatsStreamingOptions _options;

    private ConcurrentDictionary<Guid, Msg> _natsMessageStore = new();


    internal record NatsBatchContainerKey(Guid StreamId, string StreamNamespace);

    public NatsReceiver(
        string providerName,
        QueueId queueId,
        ConnectionFactory connectionFactory,
        NatsNamingConventions natsNamingConventions,
        NatsStreamingOptions natsStreamingOptions)
    {
        _providerName = providerName;
        _queueId = queueId;
        _connectionFactory = connectionFactory;
        _natsStreamConsumer = natsNamingConventions.StreamConsumerName;
        _natsStreamName = natsNamingConventions.StreamName;
        _natsSubjectPattern = natsNamingConventions.SubjectPattern;
        _options = natsStreamingOptions;
    }

    public Task Initialize(TimeSpan timeout)
    {
        _connection = _connectionFactory.CreateConnection(_options.NatsUrl);
        _jetStreamContext = _connection.CreateJetStreamContext();
        _subscription = _jetStreamContext.PullSubscribe(
            _natsSubjectPattern,
            PullSubscribeOptions.Builder()
                .WithStream(_natsStreamName)
                .WithConfiguration(
                    ConsumerConfiguration.Builder()
                        .WithName(_natsStreamConsumer)
                        .WithDurable(_natsStreamConsumer)
                        .WithAckPolicy(AckPolicy.Explicit)
                        .WithAckWait(15 * 1000)
                        .Build()
                )
                .Build()
        );
        
        return Task.CompletedTask;
    }

    public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        var result = new Dictionary<NatsBatchContainerKey, NatsBatchContainer>();

        var messages = _subscription?.Fetch(maxCount, 50);
        
        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var serializer = JsonSerializer.Create(serializerSettings);

        if (messages == null || !messages.Any())
            return Task.FromResult<IList<IBatchContainer>>(
                result.Values.Cast<IBatchContainer>().ToList()
            );
        
        foreach (var item in messages)
        {
            item.InProgress();
                
            var fullTypeNameRaw = item.Header[Constants.NatsHeaders.PayloadTypeHeader];
            var streamGuidRaw = item.Header[Constants.NatsHeaders.StreamIdHeader];
            var streamNsRaw = item.Header[Constants.NatsHeaders.StreamNamespaceHeader];

            if (fullTypeNameRaw == null || streamGuidRaw == null || streamNsRaw == null)
            {
                item.Term();
                continue;
            }

            if (!Guid.TryParse(streamGuidRaw, out var streamGuid))
            {
                item.Term();
                continue;
            }

            var key = new NatsBatchContainerKey(streamGuid, streamNsRaw);
            if (!result.TryGetValue(key, out var container))
            {
                container = new NatsBatchContainer(
                    streamGuid,
                    streamNsRaw,
                    new EventSequenceTokenV2((long)item.MetaData.StreamSequence));

                result.Add(key, container);
            }

            using var buffer = new MemoryStream(item.Data, false);
            using var bufferReader = new StreamReader(buffer);
            using var jsonReader = new JsonTextReader(bufferReader);

            var e = serializer.Deserialize(jsonReader);

            var internalId = Guid.NewGuid();
            while (_natsMessageStore.ContainsKey(internalId))
                internalId = Guid.NewGuid();
                
            container.AddEvent(internalId, e, (long)item.MetaData.ConsumerSequence);
            _natsMessageStore.AddOrUpdate(internalId, _ => item, (_, _) => item);
        }

        var batchContainers = result.Values.Cast<IBatchContainer>().ToList();
        
        return Task.FromResult<IList<IBatchContainer>>(
            batchContainers
        );
    }

    public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        var l = messages.OfType<NatsBatchContainer>().ToList();

        foreach (var entry in l.SelectMany(container => container.Entries))
        {
            if (_natsMessageStore.TryRemove(entry.InternalId, out var natsMessage))
            {
                natsMessage.Ack();
            }
        }

        return Task.CompletedTask;
    }

    public Task Shutdown(TimeSpan timeout)
    {
        if (_subscription != null)
        {
            _subscription.Unsubscribe();
            _subscription.Dispose();
        }

        _connection?.Close();
        _connection?.Dispose();
        
        return Task.CompletedTask;
    }
}