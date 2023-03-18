using Orleans;

namespace cluster1.shared;

public interface IHelloCluster1Grain : IGrainWithGuidKey
{
    public Task HelloCluster1();
}