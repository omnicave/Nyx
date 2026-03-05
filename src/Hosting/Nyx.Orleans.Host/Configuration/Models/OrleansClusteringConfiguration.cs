using JetBrains.Annotations;

namespace Nyx.Orleans.Host.Configuration.Models;

public class OrleansClusteringConfiguration
{
    public OrleansClusteringType Type { get; set; }
    public OrleansAdoNetClusteringConfiguration AdoNet { get; set; } = new();
    public OrleansDevelopmentClusteringConfiguration Development { get; set; } = new();

    public bool IsValid() => (Type == OrleansClusteringType.AdoNet && AdoNet.IsValid()) ||
                             (Type == OrleansClusteringType.Development && Development.IsValid()) ||
                             (Type == OrleansClusteringType.LocalHost && Development.IsValid());
}

[UsedImplicitly]
public class OrleansDevelopmentClusteringConfiguration
{
    public string PrimarySiloIpAddress { get; set; } = string.Empty;
    public int PrimarySiloPort { get; set; } = 0;

    internal bool IsValid() => !string.IsNullOrWhiteSpace(PrimarySiloIpAddress) && PrimarySiloPort > 0;
}

[UsedImplicitly]
public class OrleansAdoNetClusteringConfiguration
{
    public OrleansAdoNetClusteringProviderType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;

    internal string GetInvariant() => Type switch
    {
        OrleansAdoNetClusteringProviderType.SqlServer => "System.Data.SqlClient",
        OrleansAdoNetClusteringProviderType.PostgreSQL => "Npgsql",
        OrleansAdoNetClusteringProviderType.MySQL => "MySql.Data.MySqlClient",
        OrleansAdoNetClusteringProviderType.Oracle => "Oracle.ManagedDataAccess.Client",
        OrleansAdoNetClusteringProviderType.SQLite => "Microsoft.Data.Sqlite",
        _ => throw new NotSupportedException($"AdoNet storage provider type '{Type}' is not supported.")
    };

    internal bool IsValid() => !string.IsNullOrWhiteSpace(ConnectionString);
}

public enum OrleansAdoNetClusteringProviderType
{
    SqlServer,
    PostgreSQL,
    MySQL,
    Oracle,
    SQLite
}

public enum OrleansClusteringType
{
    LocalHost,
    Development,
    AdoNet,
    AzureTable,
    AzureCosmosDB,
    ZooKeeper,
    Consul
}
