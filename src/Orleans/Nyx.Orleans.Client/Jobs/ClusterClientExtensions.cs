using Orleans;

namespace Nyx.Orleans.Jobs;

public static class ClusterClientExtensions
{
    public static async Task<IBackgroundJobGrain<T>> StartJob<T>(this IClusterClient clusterClient, T jobDetails) 
        where T : class
    {
        var guid = Guid.NewGuid();
        var grain = clusterClient.GetGrain<IBackgroundJobGrain<T>>(guid);
        await grain.SetJobDetails(jobDetails);
        await grain.Start();

        return grain;
    }
}
