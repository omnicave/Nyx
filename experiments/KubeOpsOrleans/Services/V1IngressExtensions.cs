using k8s.Models;
using KubeOpsOrleans.Models;

namespace KubeOpsOrleans.Services;

public static class V1IngressExtensions
{
    public static Service ConvertIngressToHomerService(this V1Ingress ingress)
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