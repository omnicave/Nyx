using Orleans;

namespace Nyx.Orleans.Jobs;

public interface IBackgroundJobStatusGrain : IGrainWithGuidKey
{
    Task SetJobDetails(object details);
    Task<object> GetJobDetails();
    Task<JobStatus> GetJobStatus();
    Task SetJobStatus(JobStatus status);
    Task SetJobErrorInformation(JobErrorInformation errorInformation);
    Task<JobErrorInformation> GetJobErrorInformation();
}