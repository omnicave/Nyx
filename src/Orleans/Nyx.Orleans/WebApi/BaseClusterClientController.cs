using Microsoft.AspNetCore.Mvc;

namespace Nyx.Orleans.WebApi;

public abstract class BaseClusterClientController(IClusterClient clusterClient) : ControllerBase
{
    protected IClusterClient ClusterClient { get; } = clusterClient;

    protected OkObjectResult Success<T>(T value)
    {
        return Ok(Result<T>.Success(value));
    }
}