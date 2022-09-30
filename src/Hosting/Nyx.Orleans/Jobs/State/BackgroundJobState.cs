namespace Nyx.Orleans.Jobs.State;

public record BackgroundJobProgress(bool Enabled, int Min, int Max, int Current)
{
    public static readonly BackgroundJobProgress Disabled = new(false, 0, 0, 0);
}

public class BackgroundJobState
{
    public BackgroundJobState()
    {
        Status = JobStatus.Idle;
        Progress = BackgroundJobProgress.Disabled;
        JobDetails = null;
    }
    
    public object? JobDetails { get; set; } 
    
    public JobStatus Status { get; set; }
    public BackgroundJobProgress Progress { get; set; }
    public JobErrorInformation? ErrorInformation { get; set; }
}