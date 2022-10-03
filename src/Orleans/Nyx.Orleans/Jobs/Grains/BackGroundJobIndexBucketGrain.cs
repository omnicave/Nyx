using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackGroundJobIndexBucketGrain : Grain, IBackgroundJobIndexBucketGrain
{
    private readonly IPersistentState<BackgroundJobIndexBucket> _bucket;

    public BackGroundJobIndexBucketGrain(
        [PersistentState("nyx_background_jobs_idx_bucket", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobIndexBucket> bucket
    )
    {
        _bucket = bucket;
    }
    
    public Task AddJobWithId(Guid jobId)
    {
        _bucket.State.JobStatus.Add(jobId, JobStatus.Idle);
        return _bucket.WriteStateAsync();
    }

    public Task UpdateJobStatus(Guid jobId, JobStatus jobStatus)
    {
        _bucket.State.JobStatus[jobId] = jobStatus;
        return _bucket.WriteStateAsync();
    }
}