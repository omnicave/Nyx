using Orleans;
using orleans.shared;
using Orleans.Streams;

namespace orleans.server.Grains;

public class TestStreamListenerGrain : Grain, ITestStreamListenerGrain
{
    public override async Task OnActivateAsync()
    {
        var stream = GetStreamProvider("nats").GetStream<TestStreamMessage>(StreamConstants.StreamId, StreamConstants.StreamNamespace);
        Func<TestStreamMessage,StreamSequenceToken,Task> processMessages = (msg, sequence) =>
        {
            Console.WriteLine($"{msg.Message} [{msg.Number}]");
            return Task.CompletedTask;
        };
        var result = await stream.SubscribeAsync(processMessages);
        
        await base.OnActivateAsync();
    }

    public Task Start() => Task.CompletedTask;
}