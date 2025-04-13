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

    private readonly NatsNamingConventions _natsNamingConventions;
    private readonly string _natsStreamName;
    private readonly string _natsSubjectPattern;
    private readonly NatsStreamingOptions _options;
    private readonly ConcurrentDictionary<Guid, NatsJSMsg<NatsMessageEnvelope>> _natsMessageStore = new();
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
        _natsNamingConventions = natsNamingConventions;
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
        var streamConsumerName = BuildStreamConsumerName();

        string BuildStreamConsumerName()
        {
            return !string.IsNullOrEmpty(_options.ConsumerName) 
                // ? $"{_natsNamingConventions.Prefix}-{_providerName}-{_options.ConsumerName}" 
                ? $"{_options.ConsumerName}" 
                : _natsNamingConventions.StreamConsumerName;
        }

        var consumerConfig = new ConsumerConfig(streamConsumerName)
        {
            DurableName = streamConsumerName,
            AckWait = TimeSpan.FromSeconds(15)
        };
            
        consumerConfig = _options.ConsumerConfigurationBuilder(consumerConfig);
        
        // ensure we have explicit ACKs set.  The stream provider controls the status of the messages.
        consumerConfig.AckPolicy = ConsumerConfigAckPolicy.Explicit;
        
        _consumer = await _jsContext.CreateConsumerAsync(_natsStreamName, consumerConfig);
    }

    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_consumer == null)
            throw new InvalidOperationException("Consumer not set up.");
        
        var result = new Dictionary<StreamId, NatsBatchContainer>();
        
        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var natsSerializer = new NewtonsoftNatsSerializer<NatsMessageEnvelope>(serializerSettings);
        
        var messages = _consumer.FetchAsync<NatsMessageEnvelope>(new NatsJSFetchOpts()
        {
            MaxMsgs = maxCount,
        }, natsSerializer );
        await foreach (var natsMessageContainer in messages)
        {
            // inform nats that we'll start processing this message
            await natsMessageContainer.AckProgressAsync();

            // some sanity checks
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

            if (natsMessageContainer.Data?.Payload == null )
            {
                // inform NATS that we have stopped processing this message because the contents are bad
                await natsMessageContainer.AckTerminateAsync();
                continue;
            }
            
            // now let's see where we should deliver this message
            var streamId = StreamId.Create(streamNsRaw, streamKeyRaw);
            
            if (!result.TryGetValue(streamId, out var container))
            {
                container = new NatsBatchContainer(
                    streamId,
                    new EventSequenceTokenV2((long)natsMessageContainer.Metadata!.Value.Sequence.Stream)
                );

                result.Add(streamId, container);
            }

            // generate an internal id for this message
            var internalId = Guid.NewGuid();
            while (_natsMessageStore.ContainsKey(internalId))
                internalId = Guid.NewGuid();
                
            container.AddEvent(internalId, natsMessageContainer.Data.Payload, (long)natsMessageContainer.Metadata!.Value.Sequence.Consumer);
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
        await _connection.DisposeAsync();
        _consumer = null;
    }
}