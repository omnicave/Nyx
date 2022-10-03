using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJobIndexGrain : IGrainWithGuidKey
{
    Task RegisterJobWithId(Guid jobId);
    Task UpdateJobStatus(Guid jobId, JobStatus jobStatus);
    
}

public interface IBackgroundJobIndexBucketGrain : IGrainWithGuidKey
{
    Task AddJobWithId(Guid jobId);
    Task UpdateJobStatus(Guid jobId, JobStatus jobStatus);
}