using Microsoft.AspNetCore.Http.Features;

namespace Nyx.Orleans.Host;

public class OrleansHost : 
    IHost,
    IDisposable,
    IApplicationBuilder,
    IEndpointRouteBuilder,
    IAsyncDisposable
{
    private readonly WebApplication _webapp;
    internal OrleansHost(WebApplication webapp) => _webapp = webapp;
    
    public Task StartAsync(CancellationToken cancellationToken = new()) => _webapp.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = new()) => _webapp.StopAsync(cancellationToken);
    public IServiceProvider Services => _webapp.Services;
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => ((IApplicationBuilder)_webapp).Use(middleware);
    public IApplicationBuilder New() => ((IApplicationBuilder)_webapp).New();
    public RequestDelegate Build() => ((IApplicationBuilder)_webapp).Build();

    IServiceProvider IApplicationBuilder.ApplicationServices
    {
        get => ((IApplicationBuilder)_webapp).ApplicationServices;
        set => ((IApplicationBuilder)_webapp).ApplicationServices = value;
    }

    IFeatureCollection IApplicationBuilder.ServerFeatures => ((IApplicationBuilder)_webapp).ServerFeatures;
    IDictionary<string, object?> IApplicationBuilder.Properties => ((IApplicationBuilder)_webapp).Properties;
    
    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => ((IEndpointRouteBuilder)_webapp).CreateApplicationBuilder();
    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => ((IEndpointRouteBuilder)_webapp).DataSources;
    
    public IServiceProvider ServiceProvider => ((IEndpointRouteBuilder)_webapp).ServiceProvider;
    
    public void Dispose() => ((IDisposable)_webapp).Dispose();
    public ValueTask DisposeAsync() => _webapp.DisposeAsync();
}