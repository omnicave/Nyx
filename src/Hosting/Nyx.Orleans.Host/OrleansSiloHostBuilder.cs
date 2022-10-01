using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Nyx.Cli;
using Nyx.Hosting;
using Nyx.Orleans.Host.Db;
using Nyx.Orleans.Serialization;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Nyx.Orleans.Host;

public class OrleansSiloHostBuilder : BaseHostBuilder
{
    private readonly string _clusterId;
    private readonly string _serviceId;
    private readonly string[] _args;
    
    private readonly string _title;
    private readonly string _version;
    private Action<MvcNewtonsoftJsonOptions> _configureJsonSerializer = options => { };
    internal Action<HostBuilderContext, ISiloBuilder> ClusteringConfiguration = OrleansSiloHostBuilderExtensions.ConfigureOrleansForDevelopmentClustering;
    internal readonly List<Action<HostBuilderContext, ISiloBuilder>> SiloBuilderExtraConfiguration = new();
    internal Action<HostBuilderContext, ISiloBuilder> PubStoreConfiguration = (context, builder) => { };

    public static OrleansSiloHostBuilder CreateSiloHost(string clusterId, string serviceId, string? title = null, string[]? args = null)
    {
        var entryAssembly = Assembly.GetEntryAssembly() 
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        return new OrleansSiloHostBuilder(
            clusterId,
            serviceId,
            title ?? entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Orleans Host Default Name",
            args ?? Array.Empty<string>(),
            entryAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "0.1.0"
        );
    }
    
    protected OrleansSiloHostBuilder(
        string clusterId,
        string serviceId,
        string title, 
        string[] args,
        string version)
    {
        _clusterId = clusterId;
        _serviceId = serviceId;
        _title = title;
        _version = version;
        _args = args;
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

    class CliSiloHostBuilderBridge : IHostBuilder
    {
        private readonly OrleansSiloHostBuilder _parent;
        private readonly int _gatewayPort;
        private readonly int _siloPort;
        private readonly int _dashboardPort;
        private readonly int _apiPort;
        private readonly int _healthCheckPort;

        public CliSiloHostBuilderBridge(OrleansSiloHostBuilder parent, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
        {
            _parent = parent;
            _gatewayPort = gatewayPort;
            _siloPort = siloPort;
            _dashboardPort = dashboardPort;
            _apiPort = apiPort;
            _healthCheckPort = healthCheckPort;
        }

        public IHost Build() => _parent.BuildActualHost(_gatewayPort, _siloPort, _dashboardPort, _apiPort, _healthCheckPort);

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => _parent.ConfigureAppConfiguration(configureDelegate);

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => _parent.ConfigureContainer(configureDelegate);

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => _parent.ConfigureHostConfiguration(configureDelegate);

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => _parent.ConfigureServices(configureDelegate);

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) => _parent.UseServiceProviderFactory(factory);

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) => _parent.UseServiceProviderFactory(factory);

        public IDictionary<object, object> Properties => _parent.Properties;
    }

    protected IHost BuildActualHost(int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        var webApplicationBuilder = WebApplication.CreateBuilder(_args);
        
        webApplicationBuilder.Services.AddHostedService<EnsureOrleansSchemaInPgsql>();

        SetupWebApi(_title, webApplicationBuilder, apiPort, healthCheckPort);
        SetupOrleans(webApplicationBuilder.Host, gatewayPort, siloPort, dashboardPort);

        ApplyHostBuilderOperations(webApplicationBuilder.Host);
        
        var app = webApplicationBuilder.Build();
        SetupAppBuilder(app.Environment, app, healthCheckPort);

        app.MapControllers();

        return new OrleansHost(app);
    }

    public override IHost Build()
    {
        var self = this;
        return CommandLineHostBuilder.Create($"{_clusterId}.{_serviceId}", _args)
            .UseHostBuilderFactory(ctx =>
            {
                var gatewayPort = ctx.GetSingleOptionValue<int>("gatewayPort");
                var siloPort = ctx.GetSingleOptionValue<int>("siloPort");
                var dashboardPort = ctx.GetSingleOptionValue<int>("dashboardPort");
                var apiPort = ctx.GetSingleOptionValue<int>("apiPort");
                var healthCheckPort = ctx.GetSingleOptionValue<int>("healthCheckPort");
                return new CliSiloHostBuilderBridge(self, gatewayPort, siloPort, dashboardPort, apiPort, healthCheckPort);
            })
            .AddGlobalOption<int>("gatewayPort", 12000)
            .AddGlobalOption<int>("siloPort", 13000)
            .AddGlobalOption<int>("dashboardPort", 5002)
            .AddGlobalOption<int>("apiPort", 5001)
            .AddGlobalOption<int>("healthCheckPort", 5081)
            .WithRootCommandHandler(async (IHost host) =>
            {
                var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                applicationLifetime.ApplicationStopping
                    .Register(obj =>
                        {
                            var tcs = (TaskCompletionSource<object>)obj;
                            tcs.TrySetResult(null);
                        },
                        waitForStop);
                //
                await waitForStop.Task;
            })
            .Build();
    }

    private void SetupOrleans(ConfigureHostBuilder host, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002)
    {
        host.UseOrleans( (context, siloBuilder) =>
        {
            ClusteringConfiguration(context, siloBuilder);

            foreach (var item in SiloBuilderExtraConfiguration)
                item(context, siloBuilder);
            
            PubStoreConfiguration(context, siloBuilder);

            siloBuilder
                .Configure<SerializationProviderOptions>(options =>
                {
                    options.FallbackSerializationProvider = typeof(NewtonsoftJsonSerializer);
                })
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