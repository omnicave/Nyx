using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.ApplicationParts;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.Streams.Common;

namespace Nyx.Orleans.Nats.Streaming;

public class NatsClientPersistentStreamConfigurator : ClusterClientPersistentStreamConfigurator
{
    public NatsClientPersistentStreamConfigurator(
        string name, 
        IClientBuilder builder) : base(name, builder, NatsJetStreamAdapterFactory.Create)
    {
        builder.ConfigureApplicationParts(parts =>
            {
                parts.AddFrameworkPart(typeof(NatsJetStreamAdapterFactory).Assembly)
                    .AddFrameworkPart(typeof(EventSequenceTokenV2).Assembly);
            })
            .ConfigureServices(services =>
                {
                    services.ConfigureNamedOptionForLogging<NatsStreamingOptions>(name)
                        .ConfigureNamedOptionForLogging<SimpleQueueCacheOptions>(name)
                        .ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
                }
            );
    }
}