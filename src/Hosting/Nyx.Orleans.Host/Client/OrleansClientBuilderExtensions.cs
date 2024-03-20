using System.Net;
using Orleans.Configuration;

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
    
    public static IClientBuilder ConfigureForPostgresClustering(this IClientBuilder builder, 
        string connectionString)
    {
        builder.UseAdoNetClustering(options =>
        {
            options.ConnectionString = connectionString;
            options.Invariant = "Npgsql";
        });
        
        return builder;
    }
}

public static class OrleansClientHostBuilderExtensions
{
    public static TBuilder AddOrleansClusterClient<TBuilder>(this TBuilder builder, Action<IClientBuilder> configurator)
        where TBuilder : IHostBuilder
    {
        Action<HostBuilderContext, IClientBuilder> a = (_, b) => configurator(b); 
        return builder.AddOrleansClusterClient(new[] { a });
    }

    public static TBuilder AddOrleansClusterClient<TBuilder>(this TBuilder builder, IEnumerable<Action<HostBuilderContext, IClientBuilder>> configurator)
        where TBuilder : IHostBuilder
    {
        builder.UseOrleansClient((hostBuilderContext, clientBuilder) =>
        {
            foreach (var action in configurator)
            {
                action(hostBuilderContext, clientBuilder);
            }
        });
        
        return builder;
    }
    
    public static OrleansClientHostBuilder ConfigureClient(this OrleansClientHostBuilder builder, Action<HostBuilderContext, IClientBuilder> d)
    {
        if (d == null) throw new ArgumentNullException(nameof(d));
        builder.ClientExtraConfiguration.Add( d);
        return builder;
    }

    public static OrleansClientHostBuilder ConfigureClient(this OrleansClientHostBuilder builder, Action<IClientBuilder> d)
    {
        if (d == null) throw new ArgumentNullException(nameof(d));
        
        builder.ClientExtraConfiguration.Add( (_, b) => d(b));
        return builder;
    }
}