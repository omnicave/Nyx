using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nyx.WebApi.Modules;
using Scalar.AspNetCore;

namespace Nyx.WebApi;

public static class WebApplicationBuilderExtensions
{
    private static readonly Action<MvcNewtonsoftJsonOptions, Action<MvcNewtonsoftJsonOptions>?> ConfigureNewtonsoftJsonSerializerWrapper = (options, w) =>
    {
        options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
        options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
        options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        options.SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        options.SerializerSettings.Formatting = Formatting.Indented;

        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        
        w?.Invoke(options);
        
    };

    private static readonly Action<JsonOptions, Action<JsonOptions>?> ConfigureSystemTextJsonSerializerWrapper = (options, w) =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        w?.Invoke(options);
    };
    
    public static WebApplicationBuilder ConfigureWebApiWithDefaults(
        this WebApplicationBuilder builder,
        
        Action<JsonOptions>? configureSystemTextJsonSerializer = null,
        Action<MvcNewtonsoftJsonOptions>? configureNewtonsoftJsonSerializer = null
        )
    {
        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        var title = entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Orleans Host Default Name";

        builder.Services.AddControllers()
            .AddJsonOptions(opt => ConfigureSystemTextJsonSerializerWrapper(opt, configureSystemTextJsonSerializer))
            .AddNewtonsoftJson(opt => ConfigureNewtonsoftJsonSerializerWrapper(opt, configureNewtonsoftJsonSerializer));

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi();

        // swagger support
        builder.Services.AddSwaggerGen(
            c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = title, Version = "v1" });

                c.EnableAnnotations();

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
                        []
                    }
                });
            }
        );

        builder.Services.AddHealthChecks();

        // get the current list of urls we are listening on and add '5082' to it to have the health checks respond
        // on a separate port
        var httpApiPort = int.TryParse(Environment.GetEnvironmentVariable("HTTP_API_PORT"), out var ap) ? ap : 5050;
        var healthCheckPort = int.TryParse(Environment.GetEnvironmentVariable("HEALTH_CHECK_PORT"), out var hp) ? hp : Random.Shared.Next(6000, 8000);

        var currentUrls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey) ?? $"http://0.0.0.0:{httpApiPort}";
        builder.WebHost.UseUrls(
            currentUrls
                .Split(';')
                .Append($"http://0.0.0.0:{healthCheckPort}")
                .ToArray()
        );
        return builder;
    }

    public static T ConfigureWebApiDefaults<T>(this T app) where T : IApplicationBuilder
    {
        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? throw new InvalidOperationException("Entry assembly not available.");
        var title = entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Orleans Host Default Name";
        var version = entryAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "0.1.0";
        
        var healthCheckPort = int.TryParse(Environment.GetEnvironmentVariable("HTTP_API_PORT"), out var hp) ? hp : 5060;
        
        if (app is WebApplication wa)
        {
            app.UseDeveloperExceptionPage();
            wa.MapOpenApi();
            wa.MapScalarApiReference(sa =>
            {
                var scalarConfiguration = wa.Services.GetService<Action<ScalarOptions>>();
                scalarConfiguration?.Invoke(sa);
            });
            
            // run any custom configurations for a WebApplication
            var webAppConfigurators = wa.Services.GetServices<IWebAppConfigurator>();

            foreach (var item in webAppConfigurators)
                item.Configure(wa);
            
        }


        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{title} v{version}"));
        
        
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
        
        return app;
    }
}
