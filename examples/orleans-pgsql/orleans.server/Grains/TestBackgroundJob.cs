using Nyx.Orleans.Jobs;
using Nyx.Orleans.Jobs.Grains;
using orleans.shared;
using Orleans.Streams;

namespace orleans.server.Grains;

public class TestBackgroundJob : BackgroundJobGrain<TestJob>, IBackgroundJobGrain<TestJob>
{
    private IAsyncStream<TestStreamMessage>? _stream;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var sp = this.GetStreamProvider("nats");
        _stream = sp.GetStream<TestStreamMessage>(StreamConstants.StreamNamespace, StreamConstants.StreamId);

        await base.OnActivateAsync(cancellationToken);
    }

    protected override async Task WorkerAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken, TestJob testJob)
    {
        Console.WriteLine("starting ....");
        int i = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Sleepin...");
            if (_stream != null)
                await _stream.OnNextAsync(new TestStreamMessage($"Iteration {i++}", i));
            
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        
        Console.WriteLine("finishing ....");
    }
}