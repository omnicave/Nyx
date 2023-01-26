using System.Security.Cryptography;
using System.Text;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOpsOrleans.Crd;
using KubeOpsOrleans.Services;
using Orleans;

namespace KubeOpsOrleans.Grains;

public interface IHomerDeploymentGrain : IGrainWithStringKey
{
    Task Configure(HomerV1Beta homerResource);
    
    Task Refresh();
    Task<bool> IsSynced();
    Task Sync();
}

class HomerDeploymentGrain : Grain, IHomerDeploymentGrain
{
    private readonly IKubernetesClient _kubernetes;
    private readonly IHomerConfigFileGenerator _homerConfigFileGenerator;
    private readonly List<V1Ingress> _ingresses = new();
    private HomerV1Beta? _homerResource;
    private V1Deployment? _currentDeployment;
    private V1ConfigMap? _currentConfigmap;
    private string? _configFile;
    private string? _configFileHash;

    public static string HashWithSha256(string? value)
    {
        if (value == null)
            return "";
        
        using var hash = SHA256.Create();
        var byteArray = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(byteArray);
    }

    public HomerDeploymentGrain(IKubernetesClient kubernetes, IHomerConfigFileGenerator homerConfigFileGenerator)
    {
        _kubernetes = kubernetes;
        _homerConfigFileGenerator = homerConfigFileGenerator;
    }

    public Task Configure(HomerV1Beta homerResource)
    {
        _homerResource = homerResource;
        return Refresh();
    }

    public async Task Refresh()
    {
        if (_homerResource == null)
            throw new InvalidOperationException("Homer Resource Required");
        
        _currentConfigmap = await _kubernetes.Get<V1ConfigMap>( _homerResource.Name(), _homerResource.Namespace());
        _currentDeployment = await _kubernetes.Get<V1Deployment>(_homerResource.Name(), _homerResource.Namespace());

        var remoteIngresses =
            (await _kubernetes.List<V1Ingress>(_homerResource.Namespace())).ToDictionary(x => x.Name());
        
        _ingresses.Clear();
        _ingresses.AddRange(remoteIngresses.Values);

        await RebuildConfigFile();
    }

    private async Task RebuildConfigFile()
    {
        if (_homerResource == null)
            throw new InvalidOperationException("Homer Resource Required");
        
        await using var templateStream = await _homerConfigFileGenerator.RenderTemplate(_homerResource, _ingresses);
        using var reader = new StreamReader(templateStream);
        _configFile = await reader.ReadToEndAsync();
        _configFileHash = HashWithSha256(_configFile);
    }

    public Task<bool> IsSynced()
    {
        if (_homerResource == null)
            throw new InvalidOperationException("Homer Resource Required");

        if (_currentDeployment == null)
            return Task.FromResult(false);  // deployment doesn't exist

        if (_currentConfigmap == null)
        {
            return Task.FromResult(false);
        }

        var currentConfigFileHash = HashWithSha256(_currentConfigmap.Data["config.yml"]);
        if (currentConfigFileHash != HashWithSha256(_configFile))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task Sync()
    {
        if (_homerResource == null)
            throw new InvalidOperationException("Homer Resource Required");

        var configMap = (_currentConfigmap == null)
            ? BuildHomerConfigmap(_homerResource)
            : BuildHomerConfigmap(_currentConfigmap);

        _currentConfigmap = _currentConfigmap == null
            ? await _kubernetes.Create(configMap)
            : await _kubernetes.Update(configMap);

        _currentDeployment = (_currentDeployment == null)
            ? await _kubernetes.Create(BuildHomerDeploymentManifest(_homerResource))
            : await _kubernetes.Update(BuildHomerDeploymentManifest(_homerResource, _currentDeployment));
    }

    private V1ConfigMap BuildHomerConfigmap(HomerV1Beta homer)
    {
        return BuildHomerConfigmap(new V1ConfigMap()
        {
            // ApiVersion = "v1",
            // Kind = "ConfigMap",
            Metadata = new()
            {
                Name = homer.Name(),
                NamespaceProperty = homer.Namespace(),
                Labels = new Dictionary<string, string>()
                {
                    { "experiment.nyx.app/instance", _homerResource.Name() },
                    { "experiment.nyx.app/type", "host" }
                }
            },
            Data = new Dictionary<string, string>()
            {
                { "config.yml", "" }
            }
        });
    }
    private V1ConfigMap BuildHomerConfigmap(V1ConfigMap currentConfigmap)
    {
        currentConfigmap.Data["config.yml"] = _configFile;
        return currentConfigmap;
    }

    private V1Deployment BuildHomerDeploymentManifest(HomerV1Beta homer, V1Deployment deploymentManifest)
    {
        deploymentManifest.Spec.Selector = new()
        {
            MatchLabels = new Dictionary<string, string>()
            {
                { "experiment.nyx.app/instance", homer.Name() },
                { "experiment.nyx.app/type", "host" }
            }
        };
        deploymentManifest.Spec.Replicas = homer.Spec.Replicas;
        deploymentManifest.Spec.Template = BuildPodTemplate(
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
                        new("service", image: "b4bz/homer:v22.11.2")
                        {
                            VolumeMounts = new List<V1VolumeMount>()
                            {
                                new V1VolumeMount("/www/assets/config.yml", "config", subPath: "config.yml")
                            }
                        }
                    },
                    Volumes = new List<V1Volume>
                    {
                        new("config", configMap: new V1ConfigMapVolumeSource(name: _currentConfigmap.Name()))
                    }
                }
            },
            homer.Spec.PodTemplate
        );

        return deploymentManifest;
    }
    private V1Deployment BuildHomerDeploymentManifest(HomerV1Beta homer) =>
        BuildHomerDeploymentManifest(
            homer, new V1Deployment
            {
                Metadata = new()
                {
                    Name = homer.Name(),
                    NamespaceProperty = homer.Namespace()
                },
                Spec = new()
            }
        );

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
}