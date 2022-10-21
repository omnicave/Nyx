using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.ApplicationParts;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsSiloPersistentStreamConfigurator : SiloPersistentStreamConfigurator
{
    public NatsSiloPersistentStreamConfigurator(
        string name, 
        Action<Action<IServiceCollection>> configureServicesDelegate, 
        Action<Action<IApplicationPartManager>> configureAppPartsDelegate) : base(name, configureServicesDelegate, NatsJetStreamAdapterFactory.Create)
    {
        configureAppPartsDelegate(parts =>
        {
            parts
                .AddFrameworkPart(typeof(NatsJetStreamAdapterFactory).Assembly)
                .AddFrameworkPart(typeof(EventSequenceTokenV2).Assembly);
        });

        ConfigureDelegate(services =>
        {
            // services.AddOptions<NatsStreamingOptions>(name);
            // //services.AddOptions<HashRingStreamQueueMapperOptions>(name);
            
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