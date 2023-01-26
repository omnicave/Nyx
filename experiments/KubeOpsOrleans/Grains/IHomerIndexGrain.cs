using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOpsOrleans.Crd;
using Orleans;

namespace KubeOpsOrleans.Grains;

public interface IHomerIndexGrain : IGrainWithGuidKey
{
    Task AddOrUpdate(HomerV1Beta homer);
    Task<ReadOnlyCollection<HomerV1Beta>> GetAll();

    Task<HomerSettings> GetDefaultSettings();
    Task<HomerSettings> GetSettingsByName(string name);
}

class HomerIndexGrain : Grain, IHomerIndexGrain
{
    private readonly IKubernetesClient _kubernetes;
    private readonly ConcurrentDictionary<string, HomerV1Beta> _store = new();

    public HomerIndexGrain(IKubernetesClient kubernetes)
    {
        _kubernetes = kubernetes;
    }
    
    public async Task AddOrUpdate(HomerV1Beta homer)
    {
        if (_store.TryGetValue(homer.Metadata.Name, out var current))
        {
            if (current != homer)
            {
                // update
            }
        }
        else
        {
            _store[homer.Metadata.Name] = homer;
            
            // create
            var d = new V1Deployment()
            {
                Metadata = new()
                {
                    Name = homer.Name(),
                    NamespaceProperty = homer.Metadata.Namespace()
                },
                Spec = new()
                {
                    Selector = new()
                    {
                        MatchLabels = new Dictionary<string, string>()
                        {
                            { "experiment.nyx.app/instance", homer.Name() },
                            { "experiment.nyx.app/type", "host" }
                        }
                    },
                    Replicas = homer.Spec.Replicas,
                    Template = BuildPodTemplate(
                        new V1PodTemplateSpec()
                        {
                            Metadata = new()
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                    { "experiment.nyx.app/instance", homer.Name() },
                                    { "experiment.nyx.app/type", "host" }
                                }
                            },
                            Spec = new V1PodSpec()
                            {
                                Containers = new List<V1Container>
                                {
                                    new(
                                        "service", image: "b4bz/homer:v22.11.2")
                                }
                            }     
                        },
                        homer.Spec.PodTemplate
                        )
                }
            };
            await _kubernetes.Create(d);
        }
    }

    private V1PodTemplateSpec BuildPodTemplate(params V1PodTemplateSpec?[] specPodTemplate)
    {
        var prime = specPodTemplate[0];

        if (prime == null)
            throw new InvalidOperationException();
        
        var result = new V1PodTemplateSpec(prime.Metadata, prime.Spec);

        for (int i = 1; i < specPodTemplate.Length; i++)
        {
            var x = specPodTemplate[i];
            if (x == null) continue;
            
            // handle metadata
            x.Metadata.Annotations.ToList().ForEach( pair => result.Metadata.Annotations[pair.Key] = pair.Value);
            x.Metadata.Labels.ToList().ForEach( pair => result.Metadata.Labels[pair.Key] = pair.Value );
            
            // handle pod spec
            // TODO:
        }
        
        return result;
    }

    public Task<ReadOnlyCollection<HomerV1Beta>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<HomerSettings> GetDefaultSettings()
    {
        return Task.FromResult(new HomerSettings()
        {
            Columns = 3,
            ConnectivityCheck = true,
            Subtitle = "Default Subtitle",
            Title = "Default Title",
            ShowHeader = true
        });
    }

    public Task<HomerSettings> GetSettingsByName(string name)
    {
        throw new NotImplementedException();
    }
}