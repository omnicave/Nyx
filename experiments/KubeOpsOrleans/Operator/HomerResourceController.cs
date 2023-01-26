using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOpsOrleans.Crd;
using KubeOpsOrleans.Grains;
using Orleans;
using KubeOps.KubernetesClient;

namespace KubeOpsOrleans.Operator;

public class HomerResourceController : IResourceController<HomerV1Beta>
{
    private readonly IClusterClient _clusterClient;
    private readonly IKubernetesClient _kubernetesClient;

    public HomerResourceController(IClusterClient clusterClient, IKubernetesClient kubernetesClient)
    {
        _clusterClient = clusterClient;
        _kubernetesClient = kubernetesClient;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(HomerV1Beta entity)
    {
        var deploymentGrain = _clusterClient.GetGrain<IHomerDeploymentGrain>($"{entity.Namespace()}/{entity.Name()}");
        await deploymentGrain.Configure(entity);

        if (await deploymentGrain.IsSynced())
        {
            entity.Status.LastReconcileTime = DateTime.UtcNow;
            await _kubernetesClient.UpdateStatus(entity);
            return null;
        }

        await deploymentGrain.Sync();
        
        entity.Status.LastConfigUpdate = DateTime.UtcNow;
        entity.Status.LastDeploymentRestart = DateTime.UtcNow;
        await _kubernetesClient.UpdateStatus(entity);

        return null;
    }

    public Task DeletedAsync(HomerV1Beta entity)
    {
        throw new NotImplementedException();
    }
}