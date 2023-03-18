using Orleans;

namespace Nyx.Orleans.Host;

public interface IExternalOrleansClusterClientFactory
{
    IClusterClient GetClusterClient(string name);
    IClusterClient GetConnectedClusterClient(string name);
}