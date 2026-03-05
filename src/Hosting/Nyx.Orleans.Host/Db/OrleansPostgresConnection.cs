namespace Nyx.Orleans.Host.Db;

public record OrleansDatabaseConnection;

public record OrleansPostgresConnection(string ConnectionString) : OrleansDatabaseConnection;