using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nyx.Hosting.DependencyInjection;

namespace Nyx.WebApi.Minimal.Routing;

public static class Extensions
{
    public static IServiceCollection AutoRegisterEndpointsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        return services.AutoRegisterServicesFromAssemblyImplementingType<IEndpointConfiguration>(assembly);
    }

    public static T MapEndpoints<T>(this T app)
        where T : IApplicationBuilder
    {
        using var scope = app.ApplicationServices.CreateAsyncScope();
        var endpointConfigurations = scope.ServiceProvider.GetServices<IEndpointConfiguration>();
        var routeBuilder = app as IEndpointRouteBuilder ?? throw new InvalidOperationException("IApplicationBuilder must implement IEndpointRouteBuilder");
        foreach (var endpointConfiguration in endpointConfigurations)
        {
            endpointConfiguration.MapEndpoints(routeBuilder);
        }

        return app;
    }
}
