using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackgroundJobStatusGrain : Grain, IBackgroundJobStatusGrain
{
    private readonly IPersistentState<BackgroundJobState> _jobState;

    public BackgroundJobStatusGrain(
        [PersistentState("jobState", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobState> jobState
    )
    {
        _jobState = jobState;
    }

    public override async Task OnActivateAsync()
    {
        if (_jobState.State == null)
        {
            _jobState.State = new BackgroundJobState()
            {
                Status = JobStatus.Idle,
                Progress = BackgroundJobProgress.Disabled,
                JobDetails = null
            };
            await _jobState.WriteStateAsync();
        }
        
        var managementGrain = GrainFactory.GetGrain<IBackgroundJobManagementGrain>(Guid.Empty);
        await managementGrain.RegisterJobWithId(this.GetPrimaryKey());

        await base.OnActivateAsync();
    }

    public Task SetJobDetails(object details)
    {
        _jobState.State.JobDetails = details;
        return _jobState.WriteStateAsync();
    }

    public Task<object> GetJobDetails()
    {
        if (_jobState.State.JobDetails == null)
            throw new InvalidOperationException("Cannot retrieve job details because it was not set.");
        
        return Task.FromResult(_jobState.State.JobDetails);
    }

    public Task<JobStatus> GetJobStatus() => Task.FromResult(_jobState.State.Status);

    public async Task SetJobStatus(JobStatus status)
    {
        var prev = _jobState.State.Status;
        _jobState.State.Status = status;
        await _jobState.WriteStateAsync();

        var managementGrain = GrainFactory.GetGrain<IBackgroundJobManagementGrain>(Guid.Empty);
        await managementGrain.UpdateJobStatus(this.GetPrimaryKey(), status);
    }

    public Task SetJobErrorInformation(JobErrorInformation errorInformation)
    {
        _jobState.State.ErrorInformation = errorInformation;
        return _jobState.WriteStateAsync();
    }

    public Task<JobErrorInformation> GetJobErrorInformation()
    {
        return Task.FromResult(_jobState.State.ErrorInformation ?? JobErrorInformation.Empty);
    }
}