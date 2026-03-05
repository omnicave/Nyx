using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Nyx.Hosting;
using Nyx.Orleans.Host.Db;
using Nyx.Orleans.Host.Internal;
using Orleans.Configuration;
using Orleans.Serialization;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Nyx.Orleans.Host;

[Obsolete]
public partial class OrleansSiloHostBuilder : BaseHostBuilder
{
    private readonly string _clusterId;
    private readonly string _serviceId;
    
    private readonly string _title;
    private readonly string _version;
    private readonly int _gatewayPort;
    private readonly int _siloPort;
    private readonly int _dashboardPort;
    private readonly int _apiPort;
    private readonly int _healthCheckPort;
    private readonly WebApplicationBuilder _webApplicationBuilder;

    private OrleansSiloHostBuilder(
        string clusterId,
        string serviceId,
        string title, 
        string[] args,
        string version,
        int gatewayPort, int siloPort, int dashboardPort, int apiPort, int healthCheckPort)
    {
        _webApplicationBuilder = WebApplication.CreateBuilder(args);
        _clusterId = clusterId;
        _serviceId = serviceId;
        _title = title;
        _version = version;
        _gatewayPort = gatewayPort;
        _siloPort = siloPort;
        _dashboardPort = dashboardPort;
        _apiPort = apiPort;
        _healthCheckPort = healthCheckPort;
    }

    public override IDictionary<object, object> Properties => _webApplicationBuilder.Host.Properties;

    // ReSharper disable once MemberCanBePrivate.Global
    protected IHost BuildActualHost(int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        _webApplicationBuilder.Services.AddHostedService<EnsureOrleansSchemaInPgsql>();
        
        ApplyHostBuilderOperations(_webApplicationBuilder.Host);

        SetupOrleans(_webApplicationBuilder.Host, gatewayPort, siloPort, dashboardPort);
        SetupWebApi(_title, _webApplicationBuilder, apiPort, healthCheckPort);
        
        ApplyHostBuilderAppOperations(_webApplicationBuilder.Host);
        
        var app = _webApplicationBuilder.Build();
        SetupAppBuilder(app, healthCheckPort);
        app.MapControllers();

        foreach (var item in WebApplicationConfiguration)
        {
            item(app);
        }
        
        return new OrleansHost(app);
    }

    public override IHost Build() => BuildActualHost(_gatewayPort, _siloPort, _dashboardPort, _apiPort, _healthCheckPort);
}