using System.Net;
using Nyx.Orleans.Host.Internal;
using Orleans.Configuration;

namespace Nyx.Orleans.Host;

public partial class OrleansSiloHostBuilder
{
    private void SetupOrleans(ConfigureHostBuilder host, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002)
    {
        host.UseOrleans( (context, siloBuilder) =>
        {
            if (ClusteringConfiguration == null)
                throw new InvalidOperationException(
                    "Cannot start Orleans cluster because clustering configuration is not set.");
            
            ClusteringConfiguration(context, siloBuilder);

            foreach (var item in SiloBuilderExtraConfiguration)
                item(context, siloBuilder);
            
            PubStoreConfiguration(context, siloBuilder);

            siloBuilder.Services.AddOrleansSerializationDefaults();
            
            siloBuilder
                .Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                    options.GatewayPort = gatewayPort;
                    options.SiloPort = siloPort;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = _clusterId;
                    options.ServiceId = _serviceId;
                })
                .UseDashboard(options =>
                    {
                        options.Port = dashboardPort;
                    }
                );
        });
    }
}