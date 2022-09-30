using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackgroundJobManagementGrain : Grain, IBackgroundJobManagementGrain
{
    private readonly IPersistentState<BackgroundJobManagementState> _managementState;

    public BackgroundJobManagementGrain(
        [PersistentState("nyx_jobs_management_grain", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobManagementState> managementState
        )
    {
        _managementState = managementState;
    }

    public override async Task OnActivateAsync()
    {
        if (_managementState.State.IndexGrainIds.Count == 0)
        {
            _managementState.State.IndexGrainIds
                .AddRange( Enumerable.Range(0, 100).Select( _ => Guid.NewGuid()) );

            await _managementState.WriteStateAsync();
        }
        
        await base.OnActivateAsync();
    }

    private IBackgroundJobIndexGrain GetIndexGrain(Guid jobId)
    {
        var index = jobId.GetHashCode() % (_managementState.State.IndexGrainIds.Count - 1);
        var id = _managementState.State.IndexGrainIds[Math.Abs(index)];

        return GrainFactory.GetGrain<IBackgroundJobIndexGrain>(id);
    }

    // private IBackgroundJobStatusGrain GetJobStatusGrain(Guid jobId)
    // {
    //     return GrainFactory.GetGrain<IBackgroundJobStatusGrain>(jobId);
    // }

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