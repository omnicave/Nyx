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

public class NatsQueueAdapter : IQueueAdapter, IAsyncDisposable
{
    private readonly IStreamQueueMapper _streamQueueMapper;
    private readonly NatsStreamingOptions _natsStreamingOptions;
    // private readonly ConnectionFactory _connectionFactory;
    // private readonly IConnection? _managementConnection;
    // private readonly IConnection? _producerConnection;
    private readonly NatsNamingConventions _natsNamingConventions;
    // private readonly IJetStream _jetStream;
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
        
        // _connectionFactory = new ConnectionFactory();
        // _managementConnection = _connectionFactory.CreateConnection(_natsStreamingOptions.NatsUrl);
        // _producerConnection = _connectionFactory.CreateConnection(_natsStreamingOptions.NatsUrl);
        // _jetStream = _producerConnection.CreateJetStreamContext();

        _natsNamingConventions = new NatsNamingConventions(name, clusterOptions, natsStreamingOptions);


        // var jsm = _managementConnection.CreateJetStreamManagementContext();
        // var scb = StreamConfiguration.Builder()
        //     .WithName(_natsNamingConventions.StreamName)
        //     .AddSubjects(_natsNamingConventions.SubjectPattern)
        //     .WithRetentionPolicy(RetentionPolicy.WorkQueue);

        // scb = natsStreamingOptions.StreamConfigurationBuilder != null 
        //     ? natsStreamingOptions.StreamConfigurationBuilder(scb)
        //     : scb;
        // var sc = scb.Build();
        //
        // if (_jsContext.GetStreamNames().Any(x => x.Equals(_natsNamingConventions.StreamName)))
        //     jsm.UpdateStream(sc);
        // else
        //     jsm.AddStream(sc);
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        var subject = _natsNamingConventions.GetSubject(streamId);

        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var natsSerializer = new NewtonsoftNatsSerializer<T>(serializerSettings);


        // using var buffer = new MemoryStream(8 * 1024);
        // using var bufferWriter = new StreamWriter(buffer);
        // using var jsonWriter = new JsonTextWriter(bufferWriter);
        //
        // var publishOptions = PublishOptions.Builder()
        //     .WithStream(_natsNamingConventions.StreamName)
        //     .Build();

        foreach (var item in events)
        {
            var headers = new NatsHeaders
            {
                [Constants.NatsHeaders.PayloadTypeHeader] = typeof(T).FullName,
                [Constants.NatsHeaders.StreamKeyHeader] = streamId.GetKeyAsString(),
                [Constants.NatsHeaders.StreamNamespaceHeader] = streamId.GetNamespace() ?? string.Empty
            };

            // serializer.Serialize(jsonWriter, item);
            // jsonWriter.Flush();
            //
            // var natsMessage = new Msg(subject, headers, buffer.GetBuffer());
            await _jsContext.PublishAsync(
                subject,
                item,
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
        var c = new StreamConfig(_natsNamingConventions.StreamName, new[]
        {
            _natsNamingConventions.SubjectPattern
        })
        {
            Retention = StreamConfigRetention.Workqueue
        };

        if (_natsStreamingOptions.StreamConfigurationBuilder != null)
        {
            c = _natsStreamingOptions.StreamConfigurationBuilder(c);
        }

        var streamExists = false;
        await foreach (var e in _jsContext.ListStreamNamesAsync())
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