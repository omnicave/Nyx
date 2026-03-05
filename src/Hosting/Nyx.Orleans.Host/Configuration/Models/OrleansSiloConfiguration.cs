namespace Nyx.Orleans.Host.Configuration.Models;

public class OrleansSiloConfiguration : OrleansConfiguration
{
    public OrleansPubSubStoreConfiguration PubSub { get; set; } = new();

    public Dictionary<string, OrleansStorageConfiguration> GrainStores { get; set; } = new();
    public OrleansEndpointConfiguration Endpoints { get; set; } = new();

    public OrleansDashboardConfiguration Dashboard { get; set; } = new();
}

public class OrleansDashboardConfiguration
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 0;

    internal bool IsValid() => Port > 0;
}

public class OrleansPubSubStoreConfiguration : OrleansStorageConfiguration
{
    public bool Enabled { get; set; } = false;
}

