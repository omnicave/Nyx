using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackgroundJobIndexGrain : Grain, IBackgroundJobIndexGrain
{
    private readonly IPersistentState<BackgroundJobIndexState> _indexState;

    public BackgroundJobIndexGrain(
        [PersistentState("nyx_background_jobs_idx_grain", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobIndexState> indexState
    )
    {
        _indexState = indexState;
    }
    public override async Task OnActivateAsync()
    {
        if (_indexState.State.BucketBrainIds.Count == 0)
        {
            _indexState.State.BucketBrainIds
                .AddRange( Enumerable.Range(0, 100).Select( _ => Guid.NewGuid()) );

            await _indexState.WriteStateAsync();
        }
        
        await base.OnActivateAsync();
    }
    
    private IBackgroundJobIndexBucketGrain GetIndexGrain(Guid jobId)
    {
        var index = jobId.GetHashCode() % (_indexState.State.BucketBrainIds.Count - 1);
        var id = _indexState.State.BucketBrainIds[Math.Abs(index)];
        return GrainFactory.GetGrain<IBackgroundJobIndexBucketGrain>(id);
    }

    public Task RegisterJobWithId(Guid jobId)
    {
        var grain = GetIndexGrain(jobId);
        return grain.AddJobWithId(jobId);
    }

    public Task UpdateJobStatus(Guid jobId, JobStatus jobStatus)
    {
        var grain = GetIndexGrain(jobId);
        return grain.UpdateJobStatus(jobId, jobStatus);
    }
}

public class BackgroundJobManagementGrain : Grain, IBackgroundJobManagementGrain
{
    
}