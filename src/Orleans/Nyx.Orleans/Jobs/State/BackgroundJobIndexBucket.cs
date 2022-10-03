namespace Nyx.Orleans.Jobs.State;

public class BackgroundJobIndexBucket
{
    public Dictionary<Guid, JobStatus> JobStatus { get; } = new();
}