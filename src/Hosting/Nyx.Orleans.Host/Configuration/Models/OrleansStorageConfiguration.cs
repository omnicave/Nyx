using System.Net;
using StackExchange.Redis;

namespace Nyx.Orleans.Host.Configuration.Models;

public class OrleansStorageConfiguration
{
    public OrleansStorageType Type { get; set; } = OrleansStorageType.Memory;

    public OrleansAdoNetStorageConfiguration AdoNet { get; set; } = new();

    public OrleansRedisStorageConfiguration Redis { get; set; } = new();
}

public class OrleansRedisStorageConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 6379;

    public int Database { get; set; } = 0;

    internal bool IsValid() => !string.IsNullOrWhiteSpace(Host) && Port > 0 && Port <= 65535;

    internal ConfigurationOptions GetConfigurationOptions()
    {
        if (!IsValid())
            throw new InvalidOperationException("Invalid redis connection configuration");

        return new ConfigurationOptions()
        {
            EndPoints = new EndPointCollection([new DnsEndPoint(Host, Port)]),
            DefaultDatabase = Database
        };
    }
}

public class OrleansAdoNetStorageConfiguration
{
    public OrleansAdoNetStorageProviderType Type { get; set; }
    public string ConnectionString { get; set; } = string.Empty;

    internal bool IsValid() => !string.IsNullOrWhiteSpace(ConnectionString);

    internal string GetInvariant() => Type switch
    {
        OrleansAdoNetStorageProviderType.SqlServer => "System.Data.SqlClient",
        OrleansAdoNetStorageProviderType.PostgreSQL => "Npgsql",
        OrleansAdoNetStorageProviderType.MySQL => "MySql.Data.MySqlClient",
        OrleansAdoNetStorageProviderType.Oracle => "Oracle.ManagedDataAccess.Client",
        OrleansAdoNetStorageProviderType.SQLite => "Microsoft.Data.Sqlite",
        _ => throw new NotSupportedException($"AdoNet storage provider type '{Type}' is not supported.")
    };
}

public enum OrleansAdoNetStorageProviderType
{
    SqlServer,
    PostgreSQL,
    MySQL,
    Oracle,
    SQLite
}

public enum OrleansStorageType
{
    Memory,
    AdoNet,
    Redis
}
