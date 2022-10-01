using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackGroundJobIndexGrain : Grain, IBackgroundJobIndexGrain
{
    private readonly IPersistentState<BackgroundJobIndexState> _index;

    public BackGroundJobIndexGrain(
        [PersistentState("nyx_jobs_index_grain", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobIndexState> index
    )
    {
        _index = index;
    }
    
    public Task AddJobWithId(Guid jobId)
    {
        _index.State.JobStatus.Add(jobId, JobStatus.Idle);
        return _index.WriteStateAsync();
    }

    public Task UpdateJobStatus(Guid jobId, JobStatus jobStatus)
    {
        _index.State.JobStatus[jobId] = jobStatus;
        return _index.WriteStateAsync();
    }
}