namespace Nyx.Orleans.Jobs.State;

public class BackgroundJobIndexState
{
    public List<Guid> BucketBrainIds { get; } = new();
}