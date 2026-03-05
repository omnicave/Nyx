using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nyx.Hosting.DependencyInjection;
using Nyx.Orleans.Host.Configuration.Models;
using Nyx.Orleans.Host.Db;
using Nyx.Orleans.Host.Internal;
using Nyx.Orleans.Serialization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using Orleans.Serialization;
using Orleans.Storage;

namespace Nyx.Orleans.Host;

public static class HostBuilderExtensions
{
    private static void EnsureDbSchema(HashSet<OrleansDatabaseConnection> connections,
        OrleansStorageConfiguration config)
    {
        if (config is { Type: OrleansStorageType.AdoNet, AdoNet.Type: OrleansAdoNetStorageProviderType.PostgreSQL })
            connections.Add(new OrleansPostgresConnection(config.AdoNet.ConnectionString));
    }

    private static ISiloBuilder ConfigureStorage(ISiloBuilder r, string name,
        OrleansStorageConfiguration config)
    {
        return config.Type switch
        {
            OrleansStorageType.Memory => r.AddMemoryGrainStorage(name),
            OrleansStorageType.AdoNet => r.AddAdoNetGrainStorage(name, options =>
            {
                options.Invariant = config.AdoNet.GetInvariant();
                options.ConnectionString = config.AdoNet.ConnectionString;
                options.GrainStorageSerializer = new JsonGrainStorageSerializer(
                    new OrleansJsonSerializer(
                        new OptionsWrapper<OrleansJsonSerializerOptions>(new OrleansJsonSerializerOptions()
                        {
                            JsonSerializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults()
                        })
                    )
                );
            }),
            OrleansStorageType.Redis => r.AddRedisGrainStorage(name, options =>
            {
                options.ConfigurationOptions = config.Redis.GetConfigurationOptions();
                options.GrainStorageSerializer = new JsonGrainStorageSerializer(
                    new OrleansJsonSerializer(
                        new OptionsWrapper<OrleansJsonSerializerOptions>(new OrleansJsonSerializerOptions()
                        {
                            JsonSerializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults()
                        })
                    )
                );
            }),
            _ => throw new NotSupportedException(
                $"Storage '{name}' cannot be configured because type '{config.Type}' is not supported."),
        };
    }

    public static IHostBuilder ConfigureSiloHost(this IHostBuilder hostBuilder, string clusterId, string serviceId)
    {
        hostBuilder.ConfigureServices((context, collection) =>
        {
            var configSection = context.Configuration.GetSection("nyx")?.GetRequiredSection("orleans");
            var config = configSection?.Get<OrleansSiloConfiguration>();

            if (config == null)
                throw new InvalidOperationException();

            var hashSet = new HashSet<OrleansDatabaseConnection>();

            if (config.Clustering is
                { Type: OrleansClusteringType.AdoNet, AdoNet.Type: OrleansAdoNetClusteringProviderType.PostgreSQL })
                hashSet.Add(new OrleansPostgresConnection(config.Clustering.AdoNet.ConnectionString));

            if (config.PubSub.Enabled)
                EnsureDbSchema(hashSet, config.PubSub);

            foreach (var (k, v) in config.GrainStores)
                EnsureDbSchema(hashSet, v);

            foreach (var c in hashSet)
            {
                collection.AddSingleton(c);
            }

            collection.AddHostedService<EnsureOrleansSchemaInPgsql>();
        });

        hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            var configSection = context.Configuration.GetSection("nyx")?.GetRequiredSection("orleans");
            var config = configSection?.Get<OrleansSiloConfiguration>();

            if (config == null)
                throw new InvalidOperationException();

            siloBuilder.Services.AddOrleansSerializationDefaults();

            var clusteringConfig = config.Clustering.IsValid()
                ? config.Clustering
                : new OrleansClusteringConfiguration()
                {
                    Type = OrleansClusteringType.LocalHost
                };

            var r = clusteringConfig.Type switch
            {
                OrleansClusteringType.AdoNet => siloBuilder.UseAdoNetClustering(options =>
                {
                    options.Invariant = config.Clustering.AdoNet.GetInvariant();
                    options.ConnectionString = config.Clustering.AdoNet.ConnectionString;
                }),
                OrleansClusteringType.LocalHost => siloBuilder.UseLocalhostClustering(),
                OrleansClusteringType.Development => siloBuilder.UseDevelopmentClustering(options =>
                    {
                        var primarySiloEndpoint = new IPEndPoint(
                            IPAddress.Parse(config.Clustering.Development.PrimarySiloIpAddress),
                            config.Clustering.Development.PrimarySiloPort
                        );

                        options.PrimarySiloEndpoint = primarySiloEndpoint;
                    }
                ),
                _ => throw new NotSupportedException(
                    $"Clustering type '{config.Clustering.Type}' is not supported."),
            };

            if (config.PubSub.Enabled)
            {
                r = ConfigureStorage(r, "PubSubStore", config.PubSub);
            }

            foreach (var (k, v) in config.GrainStores)
                r = ConfigureStorage(r, k, v);

            r.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterId;
                options.ServiceId = serviceId;
            });

            if (config.Endpoints.IsValid())
            {
                
                
                siloBuilder
                    .Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = config.Endpoints.GetAdvertisedIpAddress();
                        options.GatewayPort = config.Endpoints.GatewayPort;
                        options.SiloPort = config.Endpoints.SiloPort;
                    });
            }
            else
            {
                
                siloBuilder.Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = config.Endpoints.GetAdvertisedIpAddress();
                    options.GatewayPort = Random.Shared.Next(12000, 12999);
                    options.SiloPort = Random.Shared.Next(12000, 12999);
                });
            }

            if (config.Dashboard.Enabled && config.Dashboard.IsValid())
                r.UseDashboard(options => { options.Port = config.Dashboard.Port; }
                );

            // below config items could be common across all types of services
            var collection = siloBuilder.Services;

            collection.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.WriteIndented = true;
                options.SerializerOptions.IncludeFields = true;
            });

            if (context.Properties.TryGetValue(typeof(IExtraSiloConfiguration), out var x) &&
                x is IExtraSiloConfiguration extraSiloConfiguration)
            {
                extraSiloConfiguration.Apply(context, siloBuilder);
            }
        });

        return hostBuilder;
    }
}
