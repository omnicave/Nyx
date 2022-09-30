namespace Nyx.Orleans.Jobs;

public enum JobStatus
{
    Idle,
    Started,
    Running,
    Cancelling,
    Finished,
    Failed
}