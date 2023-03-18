using Orleans;
using Orleans.Runtime;

namespace Nyx.Orleans.Host;

public class ClusterClientConnector : IHostedService
{
    private readonly string? _name;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClusterClientConnector> _log;

    public ClusterClientConnector(string? name, IServiceProvider serviceProvider)
    {
        _name = name;
        _serviceProvider = serviceProvider;
        _log = serviceProvider.GetRequiredService<ILogger<ClusterClientConnector>>();
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var logPurposesName = _name ?? "default";
        using var logScope = _log.BeginScope(new Dictionary<string, object>
        {
            { "ClusterName", logPurposesName }
        });
        
        _log.LogTrace("ClusterClientConnector::StartAsync({ClusterName}) >> ", logPurposesName );

        var clusterClient = GetClusterClient();
        
        const int maxTries = 10;
        for (var attempt = 0; attempt < maxTries; attempt++)
        {
            try
            {
                _log.LogTrace("Attempting connection to '{ClusterName}' ({AttemptNumber}) ...", logPurposesName, attempt);
                await clusterClient.Connect();
                _log.LogTrace("Attempting connect ... ok");
                break;
            }
            catch (Exception e)
            {
                _log.LogError(e, "Failed to connect '{ClusterName}' ({AttemptNumber}) ... retrying in 10s", logPurposesName, attempt);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
        
        _log.LogTrace("ClusterClientConnector::StartAsync({ClusterName}) << ", logPurposesName);
    }

    private IClusterClient GetClusterClient()
    {
        var clusterClient = _name == null
            ? _serviceProvider.GetRequiredService<IClusterClient>()
            : _serviceProvider.GetRequiredServiceByName<IClusterClient>(_name);
        return clusterClient;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var clusterClient = GetClusterClient();
       
        await clusterClient.Close();
        await ((ClusterClientFactory)clusterClient).ClusterClientImplementation.DisposeAsync();
    }
}