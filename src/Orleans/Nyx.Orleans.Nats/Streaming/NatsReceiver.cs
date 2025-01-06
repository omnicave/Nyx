using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Newtonsoft.Json;
using Nyx.Orleans.Serialization;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

internal class NatsReceiver : IQueueAdapterReceiver
{
    private readonly string _providerName;
    private readonly QueueId _queueId;
    // private readonly ConnectionFactory _connectionFactory;
    private readonly string _natsStreamConsumer;
    private readonly string _natsStreamName;
    private readonly string _natsSubjectPattern;
    // private IConnection? _connection = null;
    // private IJetStream? _jetStreamContext;
    // private IJetStreamPullSubscription? _subscription;
    private readonly NatsStreamingOptions _options;
    private readonly ConcurrentDictionary<Guid, NatsJSMsg<object>> _natsMessageStore = new();
    private readonly NatsConnection _connection;
    private readonly NatsJSContext _jsContext;
    private INatsJSConsumer? _consumer = null;

    public NatsReceiver(
        string providerName,
        QueueId queueId,
        NatsNamingConventions natsNamingConventions,
        NatsStreamingOptions natsStreamingOptions)
    {
        _providerName = providerName;
        _queueId = queueId;
        _natsStreamConsumer = natsNamingConventions.StreamConsumerName;
        _natsStreamName = natsNamingConventions.StreamName;
        _natsSubjectPattern = natsNamingConventions.SubjectPattern;
        _options = natsStreamingOptions;
        
        _connection = new NatsConnection(new NatsOpts()
        {
            Url = _options.NatsUrl
        });

        _jsContext = new NatsJSContext(_connection);
    }

    public async Task Initialize(TimeSpan timeout)
    {
        // _connection = _connectionFactory.CreateConnection(_options.NatsUrl);
        // _jetStreamContext = _connection.CreateJetStreamContext();
        _consumer = await _jsContext.CreateConsumerAsync(
                _natsStreamName, new ConsumerConfig(_natsStreamConsumer)
                {
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    DurableName = _natsStreamConsumer,
                    AckWait = TimeSpan.FromSeconds(15)
                });
            
            // .PullSubscribe(
            // _natsSubjectPattern,
            // PullSubscribeOptions.Builder()
            //     .WithStream(_natsStreamName)
            //     .WithConfiguration(
            //         ConsumerConfiguration.Builder()
            //             .WithName(_natsStreamConsumer)
            //             .WithDurable(_natsStreamConsumer)
            //             .WithAckPolicy(AckPolicy.Explicit)
            //             .WithAckWait(15 * 1000)
            //             .Build()
            //     )
            //     .Build()
        // );
        
        // return Task.CompletedTask;
    }

    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_consumer == null)
            throw new InvalidOperationException("Consumer not set up.");
        
        var result = new Dictionary<StreamId, NatsBatchContainer>();


        // var messages = _subscription?.Fetch(maxCount, 50);
        
        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var natsSerializer = new NewtonsoftNatsSerializer<object>(serializerSettings);
        
        var messages = _consumer.FetchAsync<object>(new NatsJSFetchOpts()
        {
            MaxMsgs = maxCount,
        }, natsSerializer );
        await foreach (var natsMessageContainer in messages)
        {
            await natsMessageContainer.AckProgressAsync();

            if (natsMessageContainer.Headers == null)
            {
                await natsMessageContainer.AckTerminateAsync();
                continue;
            }
                
            var fullTypeNameRaw = natsMessageContainer.Headers[Constants.NatsHeaders.PayloadTypeHeader].Last() ?? string.Empty;
            var streamKeyRaw = natsMessageContainer.Headers[Constants.NatsHeaders.StreamKeyHeader].Last() ?? string.Empty;
            var streamNsRaw = natsMessageContainer.Headers[Constants.NatsHeaders.StreamNamespaceHeader].Last() ?? string.Empty;

            if (string.IsNullOrEmpty(fullTypeNameRaw) || string.IsNullOrEmpty(streamNsRaw) || string.IsNullOrEmpty(streamKeyRaw))
            {
                // inform NATS that we have stopped processing this message
                await natsMessageContainer.AckTerminateAsync();
                continue;
            }
            
            var streamId = StreamId.Create(streamNsRaw, streamKeyRaw);
            
            if (!result.TryGetValue(streamId, out var container))
            {
                container = new NatsBatchContainer(
                    streamId,
                    new EventSequenceTokenV2((long)natsMessageContainer.Metadata!.Value.Sequence.Stream ));

                result.Add(streamId, container);
            }

            // using var buffer = new MemoryStream(natsMessageContainer.Data, false);
            // using var bufferReader = new StreamReader(buffer);
            // using var jsonReader = new JsonTextReader(bufferReader);
            //
            // var e = serializer.Deserialize(jsonReader);
            //
            // if (e == null)
            // {
            //     natsMessageContainer.Term();
            //     continue;
            // }

            var internalId = Guid.NewGuid();
            while (_natsMessageStore.ContainsKey(internalId))
                internalId = Guid.NewGuid();
                
            container.AddEvent(internalId, natsMessageContainer.Data!, (long)natsMessageContainer.Metadata!.Value.Sequence.Consumer);
            _natsMessageStore[internalId] = natsMessageContainer;
        }

        var batchContainers = result.Values.Cast<IBatchContainer>().ToList();
        return batchContainers;
    }

    public async Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        var l = messages.OfType<NatsBatchContainer>().ToList();

        foreach (var entry in l.SelectMany(container => container.Entries))
        {
            if (_natsMessageStore.TryRemove(entry.InternalId, out var natsMessage))
            {
                await natsMessage.AckAsync();
            }
        }
    }

    public async Task Shutdown(TimeSpan timeout)
    {
        // if (_subscription != null)
        // {
        //     _subscription.Unsubscribe();
        //     _subscription.Dispose();
        // }
        //
        // _connection?.Close();
        // _connection?.Dispose();
        await _connection.DisposeAsync();
        _consumer = null;
    }
}