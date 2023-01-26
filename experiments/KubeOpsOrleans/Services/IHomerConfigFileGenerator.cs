using System.Text;
using System.Text.Unicode;
using k8s.Models;
using KubeOpsOrleans.Crd;
using KubeOpsOrleans.Grains;
using KubeOpsOrleans.Models;
using Nyx.Utils;
using Orleans;
using Scriban;

namespace KubeOpsOrleans.Services;

public interface IHomerConfigFileGenerator
{
    Task<Stream> RenderTemplate(HomerV1Beta homerResource, IEnumerable<V1Ingress> ingresses);
    Task<Stream> RenderTemplate(HomerSettings homerSettings, IEnumerable<V1Ingress> ingresses);
}

class HomerConfigFileGenerator : IHomerConfigFileGenerator
{
    public HomerConfigFileGenerator(IClusterClient clusterClient)
    {
    }

    public Task<Stream> RenderTemplate(HomerV1Beta homerResource, IEnumerable<V1Ingress> ingresses)
    {
        return RenderTemplate(homerResource.Spec.Settings, ingresses);
    }
    
    public async Task<Stream> RenderTemplate(HomerSettings homerSettings, IEnumerable<V1Ingress> ingresses)
    {
        using var streamReader = AssemblyManifestResourceReader.GetStreamReader<HomerConfigFileGenerator>("templates.homer.yaml");

        var template = Template.ParseLiquid(await streamReader.ReadToEndAsync());

        var ctx = new ConfigurationRenderingContext(
            homerSettings,
            ingresses
                .Select(GetServiceFromIngress)  
                .AsValueCollection()
        );
        var rendered = await template.RenderAsync(ctx);

        var ms = new MemoryStream(Encoding.UTF8.GetBytes(rendered));
        return ms;
    }

    private Service GetServiceFromIngress(V1Ingress ingress)
    {
        var tlsSecrets = (ingress.Spec.Tls ?? Enumerable.Empty<V1IngressTLS>()) 
            .SelectMany(x => x.Hosts, (tlsEntry, host) => (host: host, secret: tlsEntry.SecretName))
            .ToDictionary(x => x.host, x => x.secret);

        var firstHost = ingress.Spec.Rules.First().Host;
        var firstPath = ingress.Spec.Rules.First().Http.Paths.First().Path;

        var proto = tlsSecrets.ContainsKey(firstHost) ? "https" : "http";
        return new Service(ingress.Metadata.Name, new Uri($"{proto}://{firstHost}{firstPath}"));
    }
}