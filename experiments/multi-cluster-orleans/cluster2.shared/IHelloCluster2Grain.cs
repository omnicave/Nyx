using Orleans;

namespace cluster2.shared;

public interface IHelloCluster2Grain : IGrainWithGuidKey
{
    Task HelloCluster2();
}