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
    private readonly IExternalOrleansClusterClientFactory _externalOrleansClusterClientFactory;
    private readonly ILogger<Cluster1ApiController> _log;

    public Cluster1ApiController(IClusterClient clusterClient, IExternalOrleansClusterClientFactory externalOrleansClusterClientFactory, ILogger<Cluster1ApiController> log)
    {
        _clusterClient = clusterClient;
        _externalOrleansClusterClientFactory = externalOrleansClusterClientFactory;
        _log = log;
    }
    
    [HttpGet("")]
    public async Task<ActionResult> X()
    {
        _log.Trace("ClusterStartupTask::Execute() >> ");
        var remoteClusterClient = _externalOrleansClusterClientFactory.GetClusterClient("cluster2");

        int i = 0;
        while (true)
        {
            
            _log.Trace($"ClusterStartupTask::Execute() - {++i}");

            var helloCluster2 = remoteClusterClient.GetGrain<IHelloCluster2Grain>(Guid.Empty);
            await helloCluster2.HelloCluster2();
            
            break;
        }
        
        _log.Trace("ClsuterStartupTask::Execute() << ");
        
        return Ok();
    }
}