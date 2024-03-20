using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsSiloPersistentStreamConfigurator : SiloPersistentStreamConfigurator
{
    public NatsSiloPersistentStreamConfigurator(
        string name, 
        Action<Action<IServiceCollection>> configureServicesDelegate) : base(name, configureServicesDelegate, NatsJetStreamAdapterFactory.Create)
    {
        ConfigureDelegate(services =>
        {
            services
                .ConfigureNamedOptionForLogging<NatsStreamingOptions>(name)
                .ConfigureNamedOptionForLogging<SimpleQueueCacheOptions>(name)
                .ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
        });
        
    }

    public NatsSiloPersistentStreamConfigurator WithNatsAddress(string natsUrl)
    {
        this.Configure<NatsStreamingOptions>(builder => builder.Configure(o => o.NatsUrl = natsUrl));
        return this;
    }
    
    public NatsSiloPersistentStreamConfigurator WithQueueCount(int queueCount)
    {
        this.Configure<HashRingStreamQueueMapperOptions>(builder => builder.Configure(o => o.TotalQueueCount = queueCount));
        return this;
    }
}