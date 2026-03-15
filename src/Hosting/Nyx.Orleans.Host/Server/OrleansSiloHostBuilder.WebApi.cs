using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace Nyx.Orleans.Host;

public partial class OrleansSiloHostBuilder
{
    private void SetupWebApi(string title, WebApplicationBuilder builder, int apiPort, int healthCheckPort)
    {


        builder.Services.AddControllers()
            .AddJsonOptions(_configureSystemTextJsonSerializerForWebApi)
            .AddNewtonsoftJson(_configureNewtonsoftJsonSerializerForWebApi);
        
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi();
        
        builder.Services.AddHealthChecks();
        
        // get the current list of urls we are listening on and add '5082' to it to have the health checks respond
        // on a separate port
        var currentUrls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey) ?? $"http://0.0.0.0:{apiPort}";
        builder.WebHost.UseUrls(
            currentUrls
                .Split(';')
                .Append($"http://0.0.0.0:{healthCheckPort}")
                .ToArray()
        );
    }

    private void SetupAppBuilder(WebApplication app, int healthCheckPort /* = 5081 */)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        
        app.MapScalarApiReference();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHealthChecks("/health",
            healthCheckPort,
            new HealthCheckOptions()
            {
                AllowCachingResponses = false,
                ResultStatusCodes = new Dictionary<HealthStatus, int>()
                {
                    { HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable },
                    { HealthStatus.Healthy, StatusCodes.Status200OK },
                    { HealthStatus.Degraded, StatusCodes.Status200OK },
                }
            }
        );
        
        app.UseRouting();
        app.UseEndpoints(
            builder =>
            {
                builder.MapControllers();
            });
    }
}
