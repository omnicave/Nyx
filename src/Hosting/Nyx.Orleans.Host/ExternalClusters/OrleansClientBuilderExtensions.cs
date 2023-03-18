using System.Net;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Nyx.Orleans.Host;

public static partial class OrleansClientBuilderExtensions
{
    public static TBuilder RegisterExternalOrleansCluster<TBuilder>(this TBuilder builder, string name, Action<IClientBuilder> configurator)
        where TBuilder: IHostBuilder
    {
        
        builder.ConfigureServices(
            collection =>
            {
                collection
                    .AddSingletonNamedService<IClusterClient>(name,
                        (provider, s) => new ClusterClientFactory(provider, new[] {configurator}))
                    .AddHostedService<ClusterClientConnector>(provider => new ClusterClientConnector(name, provider));

                collection.TryAddSingleton<IExternalOrleansClusterClientFactory, ExternalClusterClientFactory>();
            });

        return builder;
    }
}