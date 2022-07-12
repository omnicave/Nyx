using Orleans;
using orleans.shared;

namespace orleans.server.Grains;

public class HelloWorldGrain : Grain, IHelloWorldGrain
{
    public Task<string> GetMotd()
    {
        return Task.FromResult($"Hello world.  This is the current time: {DateTime.Now}.");
    }
}