using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Newtonsoft.Json;
using Nyx.Orleans.Serialization;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

/// <summary>
///     The NatsQueueAdapter is responsible for
///     1. creating and configuring the NATS Jet Stream,
///     2. create, manage and configure queue receivers (NATS Consumer wrappers)
///     3. publish messages on the stream
/// </summary>
public class NatsQueueAdapter : IQueueAdapter, IAsyncDisposable
{
    private readonly IStreamQueueMapper _streamQueueMapper;
    private readonly NatsStreamingOptions _natsStreamingOptions;
    private readonly NatsNamingConventions _natsNamingConventions;
    private readonly NatsConnection _connection;
    private readonly NatsJSContext _jsContext;
    private INatsJSStream? _stream = null;

    public NatsQueueAdapter(
        string name, 
        IStreamQueueMapper streamQueueMapper, 
        IOptions<ClusterOptions> clusterOptions,
        NatsStreamingOptions natsStreamingOptions)
    {
        _streamQueueMapper = streamQueueMapper;
        _natsStreamingOptions = natsStreamingOptions;
        Name = name;
        
        _connection = new NatsConnection(new NatsOpts()
        {
            Url = _natsStreamingOptions.NatsUrl
        });

        _jsContext = new NatsJSContext(_connection);
        _natsNamingConventions = new NatsNamingConventions(name, clusterOptions, natsStreamingOptions);
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        var subject = _natsNamingConventions.GetSubject(streamId);

        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var natsSerializer = new NewtonsoftNatsSerializer<NatsMessageEnvelope>(serializerSettings);

        foreach (var item in events)
        {
            if (item == null) continue;
            
            var headers = new NatsHeaders
            {
                [Constants.NatsHeaders.PayloadTypeHeader] = typeof(T).FullName,
                [Constants.NatsHeaders.StreamKeyHeader] = streamId.GetKeyAsString(),
                [Constants.NatsHeaders.StreamNamespaceHeader] = streamId.GetNamespace() ?? string.Empty
            };
            await _jsContext.PublishAsync(
                subject,
                new NatsMessageEnvelope(item),
                serializer: natsSerializer,
                headers: headers
            );
        }
        //
        // return Task.CompletedTask;
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return new NatsReceiver(
            Name,
            queueId,
            _natsNamingConventions,
            _natsStreamingOptions);
    }

    public string Name { get; }
    public bool IsRewindable { get; } = false;
    public StreamProviderDirection Direction { get; } = StreamProviderDirection.ReadWrite;

    public async Task Init(CancellationToken cancellationToken = default)
    {
        var c = new StreamConfig(
            _natsNamingConventions.StreamName,
            [
                _natsNamingConventions.SubjectPattern
            ]
        );

        c = _natsStreamingOptions.StreamConfigurationBuilder(c);

        var streamExists = false;
        await foreach (var e in _jsContext.ListStreamNamesAsync(cancellationToken: cancellationToken))
        {
            if (e.Equals(_natsNamingConventions.StreamName))
                streamExists = true;
        }

        if (streamExists)
        {
            _stream = await _jsContext.UpdateStreamAsync(c, cancellationToken);
        }
        else
        {
            _stream = await _jsContext.CreateStreamAsync(c, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}