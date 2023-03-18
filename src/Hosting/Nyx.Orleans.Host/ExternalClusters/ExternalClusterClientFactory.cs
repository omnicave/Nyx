using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Host;

public class ExternalClusterClientFactory : IExternalOrleansClusterClientFactory
{
    private readonly IServiceProvider _sp;

    public ExternalClusterClientFactory(IServiceProvider sp)
    {
        _sp = sp;
    }
    
    public IClusterClient GetClusterClient(string name)
    {
        var remoteClusterClient = _sp.GetRequiredServiceByName<IClusterClient>(name);
        return remoteClusterClient;
    }
    
    public IClusterClient GetConnectedClusterClient(string name)
    {
        var remoteClusterClient = _sp.GetRequiredServiceByName<IClusterClient>(name);
        if (!remoteClusterClient.IsInitialized)
            throw new InvalidOperationException("Cannot retrieve a cluster client which is not initialized/connected.");
        
        return remoteClusterClient;
    }
}