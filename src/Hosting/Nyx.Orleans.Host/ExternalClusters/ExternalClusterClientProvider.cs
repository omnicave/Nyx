using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Host;

public class ExternalClusterClientProvider : IExternalOrleansClusterClientProvider
{
    private readonly IServiceProvider _sp;

    public ExternalClusterClientProvider(IServiceProvider sp)
    {
        _sp = sp;
    }
    
    public IClusterClient GetClusterClient(string name)
    {
        var remoteClusterClient = _sp.GetRequiredServiceByName<IClusterClient>(name);
        return remoteClusterClient;
    }
}