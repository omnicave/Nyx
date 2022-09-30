using Nyx.Orleans.Jobs;
using Nyx.Orleans.Jobs.Grains;
using orleans.shared;

namespace orleans.server.Grains;

public class TestBackgroundJob : BackgroundJobGrain<TestJob>, IBackgroundJobGrain<TestJob>
{
    protected override async Task WorkerAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken, TestJob testJob)
    {
        Console.WriteLine("starting ....");
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Sleepin...");
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
        
        Console.WriteLine("finishing ....");
    }
}