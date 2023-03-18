using System.Net;
using Nyx.Cli;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Nyx.Orleans.Host;

public static partial class OrleansClientBuilderExtensions
{
    public static IClientBuilder UseStaticClustering(this IClientBuilder clientBuilder, IEnumerable<IPEndPoint> endpoints, string clusterId,
        string serviceId)
    {
        return clientBuilder
            .UseStaticClustering(endpoints.ToArray())
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = serviceId;
            });
    }

    public static IClientBuilder UseStaticClustering(this IClientBuilder clientBuilder, IPEndPoint endpoint,
        string clusterId,
        string serviceId)
    {
        return clientBuilder.UseStaticClustering(new[] { endpoint }, clusterId, serviceId);
    }
}

public static class OrleansClientHostBuilderExtensions
{
    public static TBuilder AddOrleansClusterClient<TBuilder>(this TBuilder builder, Action<IClientBuilder> configurator)
        where TBuilder : IHostBuilder =>
        builder.AddOrleansClusterClient(new[] { configurator });

    public static TBuilder AddOrleansClusterClient<TBuilder>(this TBuilder builder, IEnumerable<Action<IClientBuilder>> configurator)
        where TBuilder : IHostBuilder
    {

        builder.ConfigureServices(
            collection => collection
                .AddSingleton<IClusterClient>(provider => new ClusterClientFactory(provider, configurator ))
                .AddHostedService<ClusterClientConnector>(provider => new ClusterClientConnector(null, provider)));

        return builder;
    }

    public static OrleansClientHostBuilder ConfigureClient(this OrleansClientHostBuilder builder, Action<IClientBuilder> d)
    {
        builder.ClientExtraConfiguration.Add(d ?? throw new ArgumentNullException(nameof(d)));
        return builder;
    }
    
    public static OrleansClientHostBuilder ConfigureCli(this OrleansClientHostBuilder builder, Action<ICommandLineHostBuilder> d)
    {
        builder.CliExtraConfiguration.Add(d ?? throw new ArgumentNullException(nameof(d)));
        return builder;
    }
}