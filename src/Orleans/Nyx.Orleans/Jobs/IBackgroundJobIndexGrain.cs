using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJobIndexGrain : IGrainWithGuidKey
{
    Task AddJobWithId(Guid jobId);
    Task UpdateJobStatus(Guid jobId, JobStatus jobStatus);
}