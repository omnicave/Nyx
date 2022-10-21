using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsJetStreamAdapterFactory : IQueueAdapterFactory
{
    private readonly string _name;
    private readonly IServiceProvider _serviceProvider;
    private readonly SimpleQueueAdapterCache _queueAdapterCache;
    private readonly HashRingBasedStreamQueueMapper _queueMapper;
    private readonly NatsStreamingOptions _natsStreamingOptions;
    private readonly IOptions<ClusterOptions> _clusterOptions;

    public static IQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        return new NatsJetStreamAdapterFactory(
            name,
            serviceProvider
        );
    }

    private NatsJetStreamAdapterFactory(string name, IServiceProvider serviceProvider)
    {
        _name = name;
        _serviceProvider = serviceProvider;

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _clusterOptions = serviceProvider.GetRequiredService<IOptions<ClusterOptions>>();
        _natsStreamingOptions = serviceProvider.GetOptionsByName<NatsStreamingOptions>(name);
        var hashRingOptions = serviceProvider.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
        _queueAdapterCache = new SimpleQueueAdapterCache(new SimpleQueueCacheOptions(), name, loggerFactory);
        _queueMapper = new HashRingBasedStreamQueueMapper(hashRingOptions, name);
    }

    public Task<IQueueAdapter> CreateAdapter()
    {
        return Task.FromResult<IQueueAdapter>(new NatsQueueAdapter(_name, _queueMapper, _clusterOptions, _natsStreamingOptions));
    }

    public IQueueAdapterCache GetQueueAdapterCache() => _queueAdapterCache; 

    public IStreamQueueMapper GetStreamQueueMapper() => _queueMapper;

    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
    }
}
