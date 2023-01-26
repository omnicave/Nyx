using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOpsOrleans.Grains;
using Orleans;

namespace KubeOpsOrleans.Operator;

public class IngressResourceController : IResourceController<V1Ingress>
{
    private readonly IClusterClient _clusterClient;

    public IngressResourceController(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    public async Task<ResourceControllerResult?> ReconcileAsync(V1Ingress entity)
    {
        var ingressIndexGrain = _clusterClient.GetGrain<IIngressIndexGrain>(Guid.Empty);

        try
        {
            await ingressIndexGrain.AddOrUpdate(entity);
        }
        catch
        {
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(30));
        }
        return null;
    }
}