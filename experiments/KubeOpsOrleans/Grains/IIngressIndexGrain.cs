using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using k8s.Models;
using Orleans;

namespace KubeOpsOrleans.Grains;

public interface IIngressIndexGrain : IGrainWithGuidKey
{
    Task AddOrUpdate(V1Ingress v1Ingress);
    Task<ReadOnlyCollection<V1Ingress>> GetAll();
}

public class IngressIndexGrain : Grain, IIngressIndexGrain
{
    private readonly ConcurrentDictionary<string, V1Ingress> _store = new();

    public Task AddOrUpdate(V1Ingress v1Ingress)
    {
        _store.AddOrUpdate(v1Ingress.Metadata.Name, _ => v1Ingress, (_, _) => v1Ingress);
        return Task.CompletedTask;
    }

    public Task<ReadOnlyCollection<V1Ingress>> GetAll()
    {
        return Task.FromResult(_store.Values.ToList().AsReadOnly());
    }
}