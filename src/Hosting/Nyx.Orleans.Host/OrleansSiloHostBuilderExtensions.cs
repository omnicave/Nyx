using Nyx.Orleans.Host.Db;
using Orleans.Hosting;
using Orleans.Runtime.Development;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Nyx.Orleans.Host;

public static class OrleansSiloHostBuilderExtensions
{
    internal static readonly Action<HostBuilderContext,ISiloBuilder> ConfigureOrleansForDevelopmentClustering = (context, builder) =>
    {
        //var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003);
        builder.UseDevelopmentClustering(primarySiloEndpoint: null);
    };
    
    public static OrleansSiloHostBuilder ConfigureForDevelopment(this OrleansSiloHostBuilder builder)
    {
        builder.ClusteringConfiguration = ConfigureOrleansForDevelopmentClustering;
        builder.UseInMemoryPubStore();
        return builder;
    }
    
    

    public static OrleansSiloHostBuilder ConfigureForPostgresClustering(this OrleansSiloHostBuilder builder, string connectionString)
    {
        builder.ClusteringConfiguration = (context, siloBuilder) =>
        {
            siloBuilder.UseAdoNetClustering(options =>
            {
                options.ConnectionString = connectionString;
                options.Invariant = "Npgsql";
            });
        };
        builder.ConfigureServices((context, collection) =>
            collection.AddSingleton(new OrleansPostgresConnection(connectionString))
        );
        
        return builder;
    }
    
    public static OrleansSiloHostBuilder UseInMemoryPubStore(this OrleansSiloHostBuilder builder)
    {
        builder.PubStoreConfiguration = (context, siloBuilder) => siloBuilder.AddMemoryGrainStorage("PubSubStore");
        return builder;
    }    
    
    public static OrleansSiloHostBuilder UsePostgresPubSubStore(this OrleansSiloHostBuilder builder, string connectionString)
    {
        builder.PubStoreConfiguration = (context, siloBuilder) => siloBuilder.AddAdoNetGrainStorage(
            "PubSubStore",
            optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.ConnectionString = connectionString;
                    options.Invariant = "Npgsql";
                }
            )
        );

        builder.ConfigureServices((context, collection) =>
            collection.AddSingleton(new OrleansPostgresConnection(connectionString))
        );
        
        return builder;
    }
    
    public static OrleansSiloHostBuilder UsePostgresGrainStorage(this OrleansSiloHostBuilder builder, string name, string connectionString)
    {
        builder.SiloBuilderExtraConfiguration.Add(
            (context, siloBuilder) => siloBuilder.AddAdoNetGrainStorage(
                name,
                optionsBuilder => optionsBuilder.Configure(options =>
                    {
                        options.ConnectionString = connectionString;
                        options.Invariant = "Npgsql";
                        options.UseJsonFormat = true;
                    }
                )
            )
        );

        builder.ConfigureServices((context, collection) =>
            collection.AddSingleton(new OrleansPostgresConnection(connectionString))
        );
        
        return builder;
    }
    
    public static OrleansSiloHostBuilder UsePostgresInternalGrainStorage(this OrleansSiloHostBuilder builder, string connectionString) 
        => builder.UsePostgresGrainStorage(Constants.NyxInternalStorageName, connectionString);

    public static OrleansSiloHostBuilder ConfigureOrleansSilo(this OrleansSiloHostBuilder builder, Action<HostBuilderContext, ISiloBuilder> d)
    {
        builder.SiloBuilderExtraConfiguration.Add(d ?? throw new ArgumentNullException(nameof(d)));
        return builder;
    }
}