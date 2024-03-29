using Nyx.Orleans.Jobs.State;
using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Jobs.Grains;

public class BackgroundJobInformationGrain : Grain, IBackgroundJobInformationGrain
{
    private readonly IPersistentState<BackgroundJobState> _jobState;

    public BackgroundJobInformationGrain(
        [PersistentState("jobState", Constants.NyxInternalStorageName)] IPersistentState<BackgroundJobState> jobState
    )
    {
        _jobState = jobState;
    }

    IBackgroundJobIndexGrain GetIndexGrain() => GrainFactory.GetGrain<IBackgroundJobIndexGrain>(Guid.Empty);

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
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
        
        await GetIndexGrain().RegisterJobWithId(this.GetPrimaryKey());

        await base.OnActivateAsync(cancellationToken);
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
        await GetIndexGrain().UpdateJobStatus(this.GetPrimaryKey(), status);
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