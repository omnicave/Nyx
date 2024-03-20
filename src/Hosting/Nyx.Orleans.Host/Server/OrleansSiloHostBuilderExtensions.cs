using Microsoft.Extensions.Options;
using Nyx.Orleans.Host.Db;
using Nyx.Orleans.Serialization;
using Orleans.Hosting;
using Orleans.Runtime.Development;
using Orleans.Serialization;
using Orleans.Storage;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Nyx.Orleans.Host;

public static class OrleansSiloHostBuilderExtensions
{
    public static OrleansSiloHostBuilder ConfigureForDevelopment(this OrleansSiloHostBuilder builder)
    {
        builder.ConfigureClustering((_, siloBuilder) =>
            siloBuilder.UseDevelopmentClustering(primarySiloEndpoint: null)
        );
        builder.UseInMemoryPubStore();
        builder.UseInMemoryInternalGrainStorage();
        return builder;
    }


    public static OrleansSiloHostBuilder ConfigureClustering(this OrleansSiloHostBuilder builder,
        Action<HostBuilderContext, ISiloBuilder> configurator)
    {
        builder.ClusteringConfiguration = configurator;
        return builder;
    }

    public static OrleansSiloHostBuilder ConfigureClustering(this OrleansSiloHostBuilder builder,
        Action<ISiloBuilder> configurator)
    {
        return ConfigureClustering(builder, (_, sb) => configurator(sb));
    }

    public static OrleansSiloHostBuilder ConfigureForPostgresClustering(this OrleansSiloHostBuilder builder,
        Func<HostBuilderContext, string> connectionStringProc)
    {
        ConfigureClustering(builder,
            (context, siloBuilder) =>
            {
                siloBuilder.UseAdoNetClustering(options =>
                {
                    options.ConnectionString = connectionStringProc(context);
                    options.Invariant = "Npgsql";
                });
            }
        );

        builder.ConfigureServices((context, collection) =>
            collection.AddSingleton(new OrleansPostgresConnection(connectionStringProc(context)))
        );

        return builder;
    }

    public static OrleansSiloHostBuilder ConfigureForPostgresClustering(this OrleansSiloHostBuilder builder,
        string connectionString)
    {
        return ConfigureForPostgresClustering(builder, _ => connectionString);
    }

    public static OrleansSiloHostBuilder UseInMemoryPubStore(this OrleansSiloHostBuilder builder)
    {
        builder.PubStoreConfiguration = (context, siloBuilder) => siloBuilder.AddMemoryGrainStorage("PubSubStore");
        return builder;
    }

    public static OrleansSiloHostBuilder UsePostgresPubSubStore(this OrleansSiloHostBuilder builder,
        string connectionString)
    {
        return UsePostgresPubSubStore(builder, (_) => connectionString);
    }

    public static OrleansSiloHostBuilder UsePostgresPubSubStore(this OrleansSiloHostBuilder builder,
        Func<HostBuilderContext, string> connectionStringProc)
    {
        builder.PubStoreConfiguration = (context, siloBuilder) =>
        {
            var connectionString = connectionStringProc(context);

            siloBuilder.AddAdoNetGrainStorage(
                "PubSubStore",
                optionsBuilder => optionsBuilder.Configure(options =>
                    {
                        options.ConnectionString = connectionString;
                        options.Invariant = "Npgsql";
                        options.GrainStorageSerializer = new JsonGrainStorageSerializer(
                            new OrleansJsonSerializer(
                                new OptionsWrapper<OrleansJsonSerializerOptions>(new OrleansJsonSerializerOptions()
                                {
                                    JsonSerializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults()
                                })
                            )
                        );
                    }
                )
            );
        };

        builder.ConfigureServices(
            (context, collection) =>
            {
                var connectionString = connectionStringProc(context);
                collection.AddSingleton(new OrleansPostgresConnection(connectionString));
            }
        );

        return builder;
    }

    public static OrleansSiloHostBuilder UsePostgresGrainStorage(this OrleansSiloHostBuilder builder, string name,
        Func<HostBuilderContext, string> connectionStringProc)
    {
        builder.SiloBuilderExtraConfiguration.Add(
            (context, siloBuilder) => siloBuilder.AddAdoNetGrainStorage(
                name,
                optionsBuilder => optionsBuilder.Configure(options =>
                    {
                        options.ConnectionString = connectionStringProc(context);
                        options.Invariant = "Npgsql";
                        options.GrainStorageSerializer = new JsonGrainStorageSerializer(
                            new OrleansJsonSerializer(
                                new OptionsWrapper<OrleansJsonSerializerOptions>(new OrleansJsonSerializerOptions()
                                {
                                    JsonSerializerSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaults()
                                })
                            )
                        );
                    }
                )
            )
        );

        builder.ConfigureServices((context, collection) =>
            collection.AddSingleton(new OrleansPostgresConnection(connectionStringProc(context)))
        );

        return builder;
    }

    public static OrleansSiloHostBuilder UsePostgresGrainStorage(this OrleansSiloHostBuilder builder, string name,
        string connectionString)
    {
        return UsePostgresGrainStorage(builder, name, _ => connectionString);
    }

    public static OrleansSiloHostBuilder UseInMemoryGrainStorage(this OrleansSiloHostBuilder builder, string name)
    {
        builder.SiloBuilderExtraConfiguration.Add(
            (context, siloBuilder) => siloBuilder.AddMemoryGrainStorage(
                name
            )
        );
        return builder;
    }

    public static OrleansSiloHostBuilder UsePostgresInternalGrainStorage(this OrleansSiloHostBuilder builder,
        string connectionString)
        => builder.UsePostgresGrainStorage(Constants.NyxInternalStorageName, connectionString);

    public static OrleansSiloHostBuilder UseInMemoryInternalGrainStorage(this OrleansSiloHostBuilder builder)
        => builder.UseInMemoryGrainStorage(Constants.NyxInternalStorageName);


    public static OrleansSiloHostBuilder ConfigureOrleansSilo(this OrleansSiloHostBuilder builder,
        Action<HostBuilderContext, ISiloBuilder> d)
    {
        builder.SiloBuilderExtraConfiguration.Add(d ?? throw new ArgumentNullException(nameof(d)));
        return builder;
    }
}