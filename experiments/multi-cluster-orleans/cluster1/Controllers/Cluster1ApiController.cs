using cluster2.shared;
using Microsoft.AspNetCore.Mvc;
using Nyx.Orleans.Host;
using Orleans;
using Orleans.Runtime;

namespace cluster1.Controllers;

[ApiController]
[Route("api")]
public class Cluster1ApiController : Controller
{
    private readonly IClusterClient _clusterClient;
    private readonly IExternalOrleansClusterClientProvider _externalOrleansClusterClientProvider;
    private readonly ILogger<Cluster1ApiController> _log;

    public Cluster1ApiController(IClusterClient clusterClient, IExternalOrleansClusterClientProvider externalOrleansClusterClientProvider, ILogger<Cluster1ApiController> log)
    {
        _clusterClient = clusterClient;
        _externalOrleansClusterClientProvider = externalOrleansClusterClientProvider;
        _log = log;
    }
    
    [HttpGet("")]
    public async Task<ActionResult> X()
    {
        _log.LogTrace("ClusterStartupTask::Execute() >> ");
        var remoteClusterClient = _externalOrleansClusterClientProvider.GetClusterClient("cluster2");

        int i = 0;
        while (true)
        {
            
            _log.LogTrace($"ClusterStartupTask::Execute() - {++i}");

            var helloCluster2 = remoteClusterClient.GetGrain<IHelloCluster2Grain>(Guid.Empty);
            await helloCluster2.HelloCluster2();
            
            break;
        }
        
        _log.LogTrace("ClsuterStartupTask::Execute() << ");
        
        return Ok();
    }
}