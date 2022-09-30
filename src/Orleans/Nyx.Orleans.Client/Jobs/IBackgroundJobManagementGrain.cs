using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJobManagementGrain : IGrainWithGuidKey
{
    Task RegisterJobWithId(Guid jobId);
    Task UpdateJobStatus(Guid jobId, JobStatus jobStatus);
}