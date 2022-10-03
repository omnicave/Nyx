using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJob<T> where T : class
{
    Guid Id { get; }
    Task<JobStatus> GetStatus();
    Task Cancel();
}

internal class BackgroundJobClient<T> : IBackgroundJob<T> where T : class
{
    private readonly IClusterClient _clusterClient;

    public BackgroundJobClient(IClusterClient clusterClient, Guid id)
    {
        _clusterClient = clusterClient;
        Id = id;
    }
    
    public Guid Id { get; }

    public Task<JobStatus> GetStatus() => _clusterClient.GetGrain<IBackgroundJobInformationGrain>(Id).GetJobStatus();

    public Task Cancel() => _clusterClient.GetGrain<IBackgroundJobGrain<T>>(Id).Cancel();
}