using Orleans;

namespace Nyx.Orleans.Host;

public interface IExternalOrleansClusterClientProvider
{
    IClusterClient GetClusterClient(string name);
}