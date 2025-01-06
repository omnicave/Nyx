using System.Net;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;

namespace Nyx.Orleans.Nats.Clustering;

public class NatsGatewayListProvider : BaseNatsClusteringBucket, IGatewayListProvider
{
    private readonly GatewayOptions _gatewayOptions;


    public async Task InitializeGatewayListProvider()
    {
        await Init();
    }

    public async Task<IList<Uri>> GetGateways()
    {
        var entries = await GetAll();
        IList<Uri> gateways = entries 
            .Where(p => p.Entry.Status == SiloStatus.Active && p.Entry.ProxyPort != 0)
            .Select(p =>
            {
                var addr = SiloAddress.New(
                    new IPEndPoint(p.Entry.SiloAddress.Endpoint.Address, p.Entry.ProxyPort),
                    p.Entry.SiloAddress.Generation
                );
                return addr.ToGatewayUri();
            })
            .ToList();

        return gateways;

    }

    public TimeSpan MaxStaleness => _gatewayOptions.GatewayListRefreshPeriod;
    public bool IsUpdatable => true;

    public NatsGatewayListProvider(
        IOptions<GatewayOptions> gatewayOptions,
        IOptions<NatsClusteringOptions> natsClusteringOptions, 
        IOptions<ClusterOptions> clusterOptions) : base(natsClusteringOptions, clusterOptions)
    {
        _gatewayOptions = gatewayOptions.Value;
    }
}