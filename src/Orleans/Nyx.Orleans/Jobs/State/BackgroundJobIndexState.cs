namespace Nyx.Orleans.Jobs.State;

public class BackgroundJobIndexState
{
    public Dictionary<Guid, JobStatus> JobStatus { get; } = new();
}