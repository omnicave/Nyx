using System.Net;
using Orleans;
using Orleans.Configuration;

namespace Nyx.Orleans.Host;

public static class OrleansClientBuilderExtensions
{
    public static IClientBuilder UseSimplifiedClustering(this IClientBuilder clientBuilder, IEnumerable<IPEndPoint> endpoints, string clusterId,
        string serviceId)
    {
        return clientBuilder.UseStaticClustering(endpoints.ToArray())
            // // Clustering information
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = serviceId;
            });
    }

    public static IClientBuilder UseSimplifiedClustering(this IClientBuilder clientBuilder, IPEndPoint endpoint,
        string clusterId,
        string serviceId)
    {
        return clientBuilder.UseSimplifiedClustering(new[] { endpoint }, clusterId, serviceId);
    }
}