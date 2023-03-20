using Microsoft.Extensions.DependencyInjection;
using Nyx.Orleans.Nats.Clustering;
using Nyx.Orleans.Nats.Streaming;
using Orleans;
using Orleans.Hosting;
using Orleans.Messaging;

namespace Nyx.Orleans.Nats;

public static class ClusterClientExtensions
{
    public static IClientBuilder AddNatsClustering(this IClientBuilder builder)
    {
        builder.ConfigureServices((collection) =>
        {
            collection.AddSingleton<IGatewayListProvider, NatsGatewayListProvider>();
        });
        return builder;
    }
    public static IClientBuilder AddNatsStreams(this IClientBuilder builder, string name, Action<ClusterClientPersistentStreamConfigurator>? configure = null)
    {
        var configurator = new NatsClientPersistentStreamConfigurator(name,
            builder
            );
        configure?.Invoke(configurator);
        return builder;
    }
}