using System.Net;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;

namespace Nyx.Orleans.Nats.Clustering;

public class NatsGatewayListProvider : BaseNatsClusteringBucket, IGatewayListProvider
{
    private readonly GatewayOptions _gatewayOptions;


    public Task InitializeGatewayListProvider()
    {
        Init();
        
        return Task.CompletedTask;
    }

    public Task<IList<Uri>> GetGateways()
    {
        IList<Uri> gateways = MembershipEntryMap
            .Values
            .Where(p => p.entry.Status == SiloStatus.Active && p.entry.ProxyPort != 0)
            .Select(p =>
            {
                var addr = SiloAddress.New(
                    new IPEndPoint(p.entry.SiloAddress.Endpoint.Address, p.entry.ProxyPort),
                    p.entry.SiloAddress.Generation
                );
                return addr.ToGatewayUri();
            })
            .ToList();

        return Task.FromResult(gateways);

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