using System.Net;
using System.Net.NetworkInformation;

namespace Nyx.Orleans.Host.Configuration.Models;

public class OrleansEndpointConfiguration
{
    public string AdvertisedIpAddress { get; set; } = string.Empty;
    public int GatewayPort { get; set; } = 0;
    public int SiloPort { get; set; } = 0;

    internal bool IsAutoDetectedAdvertisedIp() => string.IsNullOrWhiteSpace(AdvertisedIpAddress);
    
    internal bool IsValid() => (
                                   (!string.IsNullOrWhiteSpace(AdvertisedIpAddress) && IPAddress.TryParse(AdvertisedIpAddress, out _)) 
                                   || IsAutoDetectedAdvertisedIp()
                               ) && 
                               GatewayPort > 0 
                               && SiloPort > 0;

    internal IPAddress GetAdvertisedIpAddress()
    {
        if (IsAutoDetectedAdvertisedIp())
        {
            var firstInterface = NetworkInterface.GetAllNetworkInterfaces()
                .First(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                );
            var addr = firstInterface.GetIPProperties().UnicastAddresses.First();

            return addr.Address;
        }

        if (IPAddress.TryParse(AdvertisedIpAddress, out var ipAddress))
        {
            return ipAddress;
        }

        return IPAddress.Loopback;
    }
}
