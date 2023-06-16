namespace Nyx.Orleans.Data;

public static class ClusterClientExtensions
{
    public static IQueryGrain<T> GetQueryGrain<T>(this IClusterClient clusterClient) =>
        GetQueryGrain<T>(clusterClient, Guid.NewGuid()); 
    public static IQueryGrain<T> GetQueryGrain<T>(this IClusterClient clusterClient, Guid id)
    {
        return clusterClient.GetGrain<IQueryGrain<T>>(id);
    }
}