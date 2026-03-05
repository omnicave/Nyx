using Microsoft.AspNetCore.Mvc;

namespace Shipbot.WebApi.Common;

public abstract class BaseClusterClientController(IClusterClient clusterClient) : ControllerBase
{
    protected IClusterClient ClusterClient { get; } = clusterClient;

    protected OkObjectResult Success<T>(T value)
    {
        return Ok(Result<T>.Success(value));
    }
}