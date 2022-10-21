using Orleans;

namespace orleans.shared;

public interface IHelloWorldGrain : IGrainWithGuidKey
{
    public Task<string> GetMotd();
}

public record TestJob;

public static class StreamConstants
{
    public const string StreamNamespace = "test_ns";
    public static readonly Guid StreamId = Guid.Parse("{60F11541-F8A1-40E4-A5FC-C98D7245277A}");
}

public record TestStreamMessage (string Message, int Number);

public interface ITestStreamListenerGrain : IGrainWithGuidKey
{
    Task Start();
}