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

public class OrleansSiloHostBuilder : BaseHostBuilder
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
    private Action<MvcNewtonsoftJsonOptions> _configureJsonSerializer = options => { };
    internal Action<HostBuilderContext, ISiloBuilder>? ClusteringConfiguration;
    internal readonly List<Action<HostBuilderContext, ISiloBuilder>> SiloBuilderExtraConfiguration = new();
    internal Action<HostBuilderContext, ISiloBuilder> PubStoreConfiguration = (context, builder) => { };
    internal readonly List<Action<IApplicationBuilder>> ApplicationBuilderConfiguration = new();
    private readonly WebApplicationBuilder _webApplicationBuilder;

    public static OrleansSiloHostBuilder CreateSiloHost(string clusterId, string serviceId, string? title = null, string[]? args = null, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        var entryAssembly = Assembly.GetEntryAssembly() 
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        return new OrleansSiloHostBuilder(
            clusterId,
            serviceId,
            title ?? entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Orleans Host Default Name",
            args ?? Array.Empty<string>(),
            entryAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "0.1.0",
            gatewayPort: gatewayPort,
            siloPort: siloPort,
            dashboardPort: dashboardPort,
            apiPort: apiPort,
            healthCheckPort: healthCheckPort
        );
    }
    
    public static OrleansSiloHostBuilder CreateBuilder(string[]? args, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        var entryAssembly = Assembly.GetEntryAssembly() 
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        return CreateSiloHost(
            entryAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company.ToLower() ??
            throw new InvalidOperationException(
                "[AssemblyCompanyAttribute] is missing and cannot generate orleans cluster id"),
            entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product.ToLower() ??
            throw new InvalidOperationException(
                "[AssemblyProductAttribute] is missing and cannot generate orleans service id"),
            args: args,
            gatewayPort: gatewayPort,
            siloPort: siloPort,
            dashboardPort: dashboardPort,
            apiPort: apiPort,
            healthCheckPort: healthCheckPort
        );
    }

    private OrleansSiloHostBuilder(
        string clusterId,
        string serviceId,
        string title, 
        string[] args,
        string version,
        int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
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

    public OrleansSiloHostBuilder ConfigureApplicationBuilder(Action<IApplicationBuilder> appBuilderConfiguration)
    {
        ApplicationBuilderConfiguration.Add(appBuilderConfiguration);
        return this;
    }
    private void SetupWebApi(string title, WebApplicationBuilder builder, int apiPort, int healthCheckPort)
    {
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options => _configureJsonSerializer(options));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(
            c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = title, Version = "v1" });
                
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            }
        );
        
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

    protected IHost BuildActualHost(int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        _webApplicationBuilder.Services.AddHostedService<EnsureOrleansSchemaInPgsql>();
        
        ApplyHostBuilderOperations(_webApplicationBuilder.Host);

        SetupOrleans(_webApplicationBuilder.Host, gatewayPort, siloPort, dashboardPort);
        SetupWebApi(_title, _webApplicationBuilder, apiPort, healthCheckPort);
        
        ApplyHostBuilderAppOperations(_webApplicationBuilder.Host);
        
        var app = _webApplicationBuilder.Build();
        SetupAppBuilder(app.Environment, app, healthCheckPort);
        app.MapControllers();
        foreach (var item in ApplicationBuilderConfiguration)
        {
            item(app);
        }
        
        return new OrleansHost(app);
    }

    public override IHost Build()
    {
        return BuildActualHost(_gatewayPort, _siloPort, _dashboardPort, _apiPort, _healthCheckPort);
    }

    private void SetupOrleans(ConfigureHostBuilder host, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002)
    {
        host.UseOrleans( (context, siloBuilder) =>
        {
            if (ClusteringConfiguration == null)
                throw new InvalidOperationException(
                    "Cannot start Orleans cluster because clustering configuration is not set.");
            
            ClusteringConfiguration(context, siloBuilder);

            foreach (var item in SiloBuilderExtraConfiguration)
                item(context, siloBuilder);
            
            PubStoreConfiguration(context, siloBuilder);

            siloBuilder.Services.AddOrleansSerializationDefaults();
            
            siloBuilder
                .Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                    options.GatewayPort = gatewayPort;
                    options.SiloPort = siloPort;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = _clusterId;
                    options.ServiceId = _serviceId;
                })
                .UseDashboard(options =>
                    {
                        options.Port = dashboardPort;
                    }
                );
        });
    }

    private void SetupAppBuilder(IHostEnvironment environment, IApplicationBuilder app, int healthCheckPort /* = 5081 */)
    {
        if (environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{_title} v{_version}"));

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

    public OrleansSiloHostBuilder ConfigureJsonSerializer(Action<MvcNewtonsoftJsonOptions> d)
    {
        _configureJsonSerializer = d ?? throw new ArgumentNullException(nameof(d));
        return this;
    }
}