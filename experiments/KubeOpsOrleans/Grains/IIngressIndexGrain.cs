using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using k8s.Models;
using KubeOpsOrleans.Services;
using Orleans;

namespace KubeOpsOrleans.Grains;

public interface IIngressIndexGrain : IGrainWithGuidKey
{
    Task AddOrUpdate(V1Ingress v1Ingress);
    Task<ReadOnlyCollection<V1Ingress>> GetAll();
}

public class IngressIndexGrain : Grain, IIngressIndexGrain
{
    private readonly IClusterClient _clusterClient;
    private readonly ConcurrentDictionary<string, V1Ingress> _store = new();

    public IngressIndexGrain(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    public async Task AddOrUpdate(V1Ingress v1Ingress)
    {
        if (v1Ingress.Metadata.Annotations.TryGetValue(Constants.HomerInstanceSelectorLabel, out var homerInstance))
        {
            var grain = _clusterClient.GetGrain<IHomerDeploymentGrain>(homerInstance);
            await grain.AddOrUpdateService(v1Ingress.ConvertIngressToHomerService());
        }
    }

    public Task<ReadOnlyCollection<V1Ingress>> GetAll()
    {
        return Task.FromResult(_store.Values.ToList().AsReadOnly());
    }
}