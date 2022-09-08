using System.Data;
using System.Reflection;
using Npgsql;

namespace Nyx.Orleans.Host.Db;

public class EnsureOrleansSchemaInPgsql : IHostedService
{
    private readonly IEnumerable<OrleansPostgresConnection> _postgresConnections;

    public EnsureOrleansSchemaInPgsql(IEnumerable<OrleansPostgresConnection> postgresConnections)
    {
        _postgresConnections = postgresConnections;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var uniqueConnections = _postgresConnections
            .Select(x => x.ConnectionString)
            .Distinct();
        
        foreach (var c in uniqueConnections)
            await CheckSchemaAndApply(c);
    }

    private async Task CheckSchemaAndApply(string connectionString)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var selectedDatabase = connectionStringBuilder.Database;
        
        var c = new NpgsqlConnection(connectionString);
        await c.OpenAsync();
        var commmand = c.CreateCommand();
        commmand.CommandText = "SELECT * FROM pg_tables;";

        await using var reader = await commmand.ExecuteReaderAsync();

        var skipMainSql = false;
        var skipClusteringSql = false;
        var skipPersistenceSql = false;
        var skipRemindersSql = false;

        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString("tablename");
            if (tableName.Equals("OrleansQuery", StringComparison.OrdinalIgnoreCase))
                skipMainSql = true;

            if (tableName.Equals("OrleansMembershipTable", StringComparison.OrdinalIgnoreCase))
                skipClusteringSql = true;
            
            if (tableName.Equals("OrleansStorage", StringComparison.OrdinalIgnoreCase))
                skipPersistenceSql = true;

            if (tableName.Equals("OrleansRemindersTable", StringComparison.OrdinalIgnoreCase))
                skipRemindersSql = true;
        }

        await reader.CloseAsync();

        if (!skipMainSql) await ApplySqlFile(c, "PostgreSQL-Main.sql");
        if (!skipClusteringSql) await ApplySqlFile(c, "PostgreSQL-Clustering.sql");
        if (!skipPersistenceSql) await ApplySqlFile(c, "PostgreSQL-Persistence.sql");
        if (!skipRemindersSql) await ApplySqlFile(c, "PostgreSQL-Reminders.sql");
    }

    private async Task ApplySqlFile(NpgsqlConnection npgsqlConnection, string postgresqlMainSql)
    {
        var assembly = typeof(EnsureOrleansSchemaInPgsql).GetTypeInfo().Assembly;
        using var resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.sql.postgres.{postgresqlMainSql}");

        if (resource == null)
            throw new InvalidOperationException();

        using var reader = new StreamReader(resource);
        
        var command = npgsqlConnection.CreateCommand();
        command.CommandText = await reader.ReadToEndAsync();
        await command.ExecuteNonQueryAsync();

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}