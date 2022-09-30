using Orleans;

namespace orleans.shared;

public interface IHelloWorldGrain : IGrainWithGuidKey
{
    public Task<string> GetMotd();
}

public record TestJob;