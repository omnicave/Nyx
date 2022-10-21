using Microsoft.Extensions.DependencyInjection;
using Nyx.Orleans.Nats.Clustering;
using Nyx.Orleans.Nats.Streaming;
using Orleans;
using Orleans.Hosting;

namespace Nyx.Orleans.Nats;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddNatsStreams(this ISiloBuilder builder, string name, Action<NatsSiloPersistentStreamConfigurator>? configure = null)
    {
        var configurator = new NatsSiloPersistentStreamConfigurator(name,
            configureServicesDelegate => builder.ConfigureServices(configureServicesDelegate),
            configureAppPartsDelegate => builder.ConfigureApplicationParts(configureAppPartsDelegate));
        configure?.Invoke(configurator);
        return builder;
    }

    public static ISiloBuilder AddNatsClustering(this ISiloBuilder builder)
    {
        builder.ConfigureServices(
            (context, collection) =>
            {
                collection.AddSingleton<IMembershipTable, NatsMembershipTable>();
            }
        );
        
        return builder;
    }
}