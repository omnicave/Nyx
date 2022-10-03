using Orleans;

namespace Nyx.Orleans.Jobs;

public static class ClusterClientExtensions
{
    public static async Task<IBackgroundJob<T>> StartJob<T>(this IClusterClient clusterClient, T jobDetails) 
        where T : class
    {
        var guid = Guid.NewGuid();
        var infoGrain = clusterClient.GetGrain<IBackgroundJobInformationGrain>(guid);
        await infoGrain.SetJobDetails(jobDetails);
        
        var jobGrain = clusterClient.GetGrain<IBackgroundJobGrain<T>>(guid);
        await jobGrain.Start();

        return new BackgroundJobClient<T>(clusterClient, guid);
    }
}
