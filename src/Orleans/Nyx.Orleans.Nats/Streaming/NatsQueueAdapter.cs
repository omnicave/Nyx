using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.JetStream;
using Newtonsoft.Json;
using Nyx.Orleans.Serialization;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsQueueAdapter : IQueueAdapter, IDisposable
{
    private readonly IStreamQueueMapper _streamQueueMapper;
    private readonly NatsStreamingOptions _natsStreamingOptions;
    private readonly ConnectionFactory _connectionFactory;
    private readonly IConnection? _managementConnection;
    private readonly IConnection? _producerConnection;
    private readonly NatsNamingConventions _natsNamingConventions;
    private readonly IJetStream _jetStream;

    public NatsQueueAdapter(string name, IStreamQueueMapper streamQueueMapper, IOptions<ClusterOptions> clusterOptions, NatsStreamingOptions natsStreamingOptions)
    {
        _streamQueueMapper = streamQueueMapper;
        _natsStreamingOptions = natsStreamingOptions;
        Name = name;
        
        _connectionFactory = new ConnectionFactory();
        _managementConnection = _connectionFactory.CreateConnection(_natsStreamingOptions.NatsUrl);
        _producerConnection = _connectionFactory.CreateConnection(_natsStreamingOptions.NatsUrl);
        _jetStream = _producerConnection.CreateJetStreamContext();

        _natsNamingConventions = new NatsNamingConventions(name, clusterOptions);
        
        var jsm = _managementConnection.CreateJetStreamManagementContext();
        var sc = StreamConfiguration.Builder()
            .WithName(_natsNamingConventions.StreamName)
            .AddSubjects(_natsNamingConventions.SubjectPattern)
            .WithRetentionPolicy(RetentionPolicy.WorkQueue)
            .Build();

        if (jsm.GetStreamNames().Any(x => x.Equals(_natsNamingConventions.StreamName)))
            jsm.UpdateStream(sc);
        else
            jsm.AddStream(sc);
    }
    
    public Task QueueMessageBatchAsync<T>(
        Guid streamGuid, 
        string streamNamespace, 
        IEnumerable<T> events, 
        StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        var subject = _natsNamingConventions.GetSubject(streamGuid, streamNamespace);

        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var serializer = JsonSerializer.Create(serializerSettings);

        using var buffer = new MemoryStream(8*1024);
        using var bufferWriter = new StreamWriter(buffer);
        using var jsonWriter = new JsonTextWriter(bufferWriter);

        var publishOptions = PublishOptions.Builder()
            .WithStream(_natsNamingConventions.StreamName)
            .Build();
        
        foreach (var item in events)
        {
            var headers = new MsgHeader
            {
                [Constants.NatsHeaders.PayloadTypeHeader] = typeof(T).FullName,
                [Constants.NatsHeaders.StreamIdHeader] = streamGuid.ToString("N"),
                [Constants.NatsHeaders.StreamNamespaceHeader] = streamNamespace
            };

            serializer.Serialize(jsonWriter, item);
            jsonWriter.Flush();
            
            var natsMessage = new Msg(subject, headers, buffer.GetBuffer());
            _jetStream.Publish(
                natsMessage,
                publishOptions
            );
        }
        return Task.CompletedTask;
    }
    
    public Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        var subject = _natsNamingConventions.GetSubject(streamId);

        var serializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults();
        var serializer = JsonSerializer.Create(serializerSettings);

        using var buffer = new MemoryStream(8*1024);
        using var bufferWriter = new StreamWriter(buffer);
        using var jsonWriter = new JsonTextWriter(bufferWriter);

        var publishOptions = PublishOptions.Builder()
            .WithStream(_natsNamingConventions.StreamName)
            .Build();
        
        foreach (var item in events)
        {
            var headers = new MsgHeader
            {
                [Constants.NatsHeaders.PayloadTypeHeader] = typeof(T).FullName,
                [Constants.NatsHeaders.StreamIdHeader] = streamId.ToString(),
                [Constants.NatsHeaders.StreamNamespaceHeader] = streamId.GetNamespace() ?? string.Empty
            };

            serializer.Serialize(jsonWriter, item);
            jsonWriter.Flush();
            
            var natsMessage = new Msg(subject, headers, buffer.GetBuffer());
            _jetStream.Publish(
                natsMessage,
                publishOptions
            );
        }
        return Task.CompletedTask;
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return new NatsReceiver(
            Name, 
            queueId,
            _connectionFactory, 
            _natsNamingConventions,
            _natsStreamingOptions);
    }

    public string Name { get; }
    public bool IsRewindable { get; } = false;
    public StreamProviderDirection Direction { get; } = StreamProviderDirection.ReadWrite;

    public void Dispose()
    {
        _managementConnection?.Dispose();
        _producerConnection?.Dispose();
    }
}