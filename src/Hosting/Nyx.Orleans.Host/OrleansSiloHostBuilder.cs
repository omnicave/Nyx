using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
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
    private readonly string[]? _args;
    
    private readonly string _title;
    private readonly string _version;
    private Action<MvcNewtonsoftJsonOptions> _configureJsonSerializer = options => { };
    internal Action<HostBuilderContext, ISiloBuilder> ClusteringConfiguration = OrleansSiloHostBuilderExtensions.ConfigureOrleansForDevelopmentClustering;
    internal readonly List<Action<HostBuilderContext, ISiloBuilder>> SiloBuilderExtraConfiguration = new();
    internal Action<HostBuilderContext, ISiloBuilder> PubStoreConfiguration = (context, builder) => { };
    //internal readonly List<Action<IServiceCollection>> HostServiceConfiguration = new();

    public OrleansSiloHostBuilder(
        string clusterId,
        string serviceId,
        string? title = null, 
        string[]? args = null)
    {
        _clusterId = clusterId;
        _serviceId = serviceId;
        _args = args;
        
        var entryAssembly = Assembly.GetEntryAssembly() 
                            ?? throw new InvalidOperationException("Entry assembly not available.");
        
        _title = title 
                 ?? entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title 
                 ?? "Orleans Host Default Name";
        _version = entryAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version 
                   ?? "0.1.0";
    }
    
    private void SetupWebApi(string title, WebApplicationBuilder builder)
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
        var currentUrls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey) ?? "http://0.0.0.0:5001";
        builder.WebHost.UseUrls(
            currentUrls
                .Split(';')
                .Append("http://0.0.0.0:5081")
                .ToArray()
        );
    }

    public override IHost Build()
    {
        var webApplicationBuilder = _args == null ? WebApplication.CreateBuilder() : WebApplication.CreateBuilder(_args);
        
        webApplicationBuilder.Services.AddHostedService<EnsureOrleansSchemaInPgsql>();
        
        ApplyHostBuilderOperations(webApplicationBuilder.Host);
        
        SetupWebApi(_title, webApplicationBuilder);
        SetupOrleans(_title, webApplicationBuilder.Host);
        
        var app = webApplicationBuilder.Build();
        SetupAppBuilder(app.Environment, app);

        app.MapControllers();

        return new OrleansHost(app);
    }

    private void SetupOrleans(string title, ConfigureHostBuilder host)
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
                    options.GatewayPort = 12000;
                    options.SiloPort = 13000;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = _clusterId;
                    options.ServiceId = _serviceId;
                })
                .UseDashboard(options =>
                    {
                        options.Port = 5002;
                    }
                );
        });
    }

    private void SetupAppBuilder(IHostEnvironment environment, IApplicationBuilder app)
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
            5081,
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