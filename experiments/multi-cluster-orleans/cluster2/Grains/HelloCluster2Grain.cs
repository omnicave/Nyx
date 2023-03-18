using cluster2.shared;
using Orleans;

namespace cluster2.Grains;

public class HelloCluster2Grain : Grain, IHelloCluster2Grain
{
    public Task HelloCluster2()
    {
        Console.WriteLine("Hello Cluster 2 Called");
        return Task.CompletedTask;
    }
}