using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Nyx.Orleans.Jobs.Grains;

public abstract class BackgroundJobGrain<TJobDetails> : Grain, IBackgroundJobGrain<TJobDetails> 
    where TJobDetails : class
{
    private record BackgroundJobContext(
        IServiceProvider ServiceProvider, 
        CancellationToken CancellationToken,
        Guid GrainId, 
        TaskScheduler OrleansTaskScheduler, 
        TJobDetails JobDetails
        );
    
    private CancellationTokenSource? _cancellationTokenSource = null;
    private Task? _task = null;
    private IServiceScope? _scope = null;

    public Task SetJobDetails(TJobDetails details)
    {
        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        return jobStatusGrain.SetJobDetails(details);
    }

    public async Task Start()
    {
        if (_task != null)
            throw new InvalidOperationException("Cannot start a job that's already running.");

        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        if ((await jobStatusGrain.GetJobDetails()) is not TJobDetails details)
            throw new InvalidOperationException("Retrieved job details but they are not of the correct type.");
        
        _scope = ServiceProvider.CreateScope();
        _cancellationTokenSource = new CancellationTokenSource();
        var orleansTaskScheduler = TaskScheduler.Current;
        
        _task = Task.Factory.StartNew(
            async o =>
            {
                var ctx = (BackgroundJobContext)(o ?? throw new InvalidOperationException());
                await WorkerInvoker(ctx.ServiceProvider, ctx.CancellationToken, ctx.GrainId, ctx.OrleansTaskScheduler, ctx.JobDetails);
            },
            new BackgroundJobContext(
                _scope.ServiceProvider, 
                _cancellationTokenSource.Token, 
                this.GetPrimaryKey(), 
                orleansTaskScheduler,
                details
                ),
            _cancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).Unwrap();
    }

    public async Task Cancel()
    {
        if (_cancellationTokenSource == null)
            throw new InvalidOperationException("Cannot cancel a background job that is not running.");

        await Notify(JobStatus.Cancelling);
        
        _cancellationTokenSource.Cancel();
    }

    public Task<JobStatus> GetStatus()
    {
        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        return jobStatusGrain.GetJobStatus();
    }

    public Task<JobErrorInformation> GetErrorInformation()
    {
        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        return jobStatusGrain.GetJobErrorInformation();
    }

    private async Task Notify(JobStatus status)
    {
        Console.WriteLine($"Setting status '{status}'");
        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        await jobStatusGrain.SetJobStatus(status);

        switch (status)
        {
            case JobStatus.Finished:
            case JobStatus.Failed:
                _scope?.Dispose();
                _task?.Dispose();
                _cancellationTokenSource?.Dispose();

                _scope = null;
                _task = null;
                _cancellationTokenSource = null;
                
                this.DeactivateOnIdle();
                break;
        }
    }

    private async Task SetErrorInformation(JobErrorInformation errorInformation)
    {
        var jobStatusGrain = GrainFactory.GetGrain<IBackgroundJobStatusGrain>(this.GetPrimaryKey());
        await jobStatusGrain.SetJobErrorInformation(errorInformation);
    }
    
    protected virtual async Task WorkerInvoker(
        IServiceProvider serviceProvider, 
        CancellationToken cancellationToken, 
        Guid grainId, 
        TaskScheduler orleansTaskScheduler,
        TJobDetails jobDetails
        )
    {
        Task NotifyOnOrleansScheduler(JobStatus status, Exception? e = null)
        {
            var errorInfo = e != null
                ? new JobErrorInformation(
                    e.GetType().FullName ?? "-",
                    e.Message,
                    e.Source ?? string.Empty,
                    e.StackTrace ?? string.Empty,
                    e.InnerException != null
                        ? new JobErrorInformation(e.InnerException.GetType().FullName ?? "-", e.InnerException.Message,
                            e.InnerException.Source ?? string.Empty, e.InnerException.StackTrace ?? string.Empty)
                        : null
                )
                : null;
            
            var task = Task.Factory.StartNew(
                async s =>
                {
                    var p =((JobStatus status, JobErrorInformation? errorInfo))(s ?? throw new InvalidOperationException());
                    
                    if (p.errorInfo != null)
                        await SetErrorInformation(p.errorInfo);
                    
                    await Notify(p.status);
                },
                (status, errorInfo),
                CancellationToken.None,
                TaskCreationOptions.None,
                orleansTaskScheduler
            ).Unwrap();

            return task;
        }
        
        await NotifyOnOrleansScheduler(JobStatus.Started);
        await using var clusterClient = serviceProvider.GetRequiredService<IClusterClient>();
        try
        {
            await NotifyOnOrleansScheduler(JobStatus.Running);
            await WorkerAsync(serviceProvider, cancellationToken, jobDetails);
            await NotifyOnOrleansScheduler(JobStatus.Finished);
        }
        catch (Exception e)
        {
            await NotifyOnOrleansScheduler(JobStatus.Failed, e);
        }
    }

    protected abstract Task WorkerAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken, TJobDetails jobDetails);
}