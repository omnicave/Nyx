using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nyx.Hosting.DependencyInjection;
using Nyx.Hosting.Modules;
using Nyx.Orleans.Host;
using Nyx.WebApi;
using Nyx.WebApi.Minimal.Routing;
using Nyx.WebApi.Modules;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orleans.Serialization;

namespace Nyx.Microservice.Host;

public static class Extensions
{
    public static WebApplicationBuilder ConfigureNyxWebApiMicroservice(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddYamlFile("appsettings.yaml", true, true);
        builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName.ToLower()}.yaml",
            true, true);

        builder.ConfigureWebApiWithDefaults(configureNewtonsoftJsonSerializer: options =>
        {
            options.SerializerSettings.Converters.Add(new IPAddressConverter());
            options.SerializerSettings.Converters.Add(new IPEndPointConverter());
        });

        var collection = builder.Services;
        
        // service discovery
        collection.AddServiceDiscovery();
        collection.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });
        
        // open telemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        
        var otelBuilder = collection.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .SetExemplarFilter(ExemplarFilterType.TraceBased);
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());

                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation
                    // (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

        if (useOtlpExporter)
        {
            collection.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            collection.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            collection.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());

            otelBuilder.UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Prometheus exporter
        // (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // collection.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

        builder.Services.AddHttpClient();
        
        return builder;
    }
    
    public static WebApplicationBuilder ConfigureNyxMicroservice(
        this WebApplicationBuilder builder, 
        string clusterId,
        string serviceId
        )
    {
        builder.Host.ConfigureSiloHost(clusterId, serviceId);
        return builder.ConfigureNyxWebApiMicroservice();
    }

    public static WebApplicationBuilder ConfigureSilo(this WebApplicationBuilder builder,
        Action<HostBuilderContext, ISiloBuilder> configurationDelegate)
    {
        if (builder.Host.Properties.TryGetValue(typeof(IExtraSiloConfiguration), out var x) &&
            x is IExtraSiloConfiguration extra)
            builder.Host.Properties[typeof(IExtraSiloConfiguration)] =
                new ExtraSiloConfiguration(configurationDelegate, extra);
        else
            builder.Host.Properties[typeof(IExtraSiloConfiguration)] =
                new ExtraSiloConfiguration(configurationDelegate);

        return builder;
    }

    public static WebApplicationBuilder ConfigureModule<T>(this WebApplicationBuilder builder)
        where T : INyxModule, new()
    {
        var moduleAssembly = typeof(T).Assembly;
        var nyxModule = new T();
        
        builder.Services.AutoRegisterServicesFromAssembly(moduleAssembly);
        builder.Services.AutoRegisterEndpointsFromAssembly(moduleAssembly);

        if (
            builder.Host.Properties.TryGetValue(typeof(IMvcCoreBuilder), out var mvcBuilder) &&
            mvcBuilder is IMvcCoreBuilder mvcb)
        {
            mvcb.AddApplicationPart(moduleAssembly);
        }
        else
        {
            mvcb = builder.Services.AddMvcCore();
            mvcb.AddApplicationPart(moduleAssembly);

            builder.Host.Properties[typeof(IMvcCoreBuilder)] = mvcb;
        }

        if (nyxModule is IServiceRegistrationsBuilder srb)
            builder.Host.ConfigureServices((context, services) => srb.ConfigureServices(context, services));

        if (nyxModule is IWebAppConfigurator appConfigurator)
            builder.Services.AddSingleton<IWebAppConfigurator>(appConfigurator);

        if (nyxModule is IConfigurationValidationProvider cvp)
            cvp.ValidateConfiguration(builder.Configuration);

        return builder;
    }

    public static WebApplication UseNyxMicroservice(this WebApplication app)
    {
        app.ConfigureWebApiDefaults();

        return app;
    }
}
