namespace Nyx.Orleans.Jobs.State;

public class BackgroundJobManagementState
{
    public List<Guid> IndexGrainIds { get; } = new();
}