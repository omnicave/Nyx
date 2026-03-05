using Microsoft.AspNetCore.Routing;

namespace Nyx.WebApi.Minimal.Routing;

public interface IEndpointConfiguration
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
