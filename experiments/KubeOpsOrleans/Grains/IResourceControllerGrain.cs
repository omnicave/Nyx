using k8s;

namespace KubeOpsOrleans.Grains;

public interface IResourceControllerGrain<TEntity>
    where TEntity : IKubernetesObject, new()
{
    /// <summary>
    ///     Invoked when resource is first seen or an event received that it was updated.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    Task InitializeOrUpdate(TEntity resource);
}