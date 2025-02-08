using System.Reflection;

namespace Nyx.Orleans.Host;

public partial class OrleansSiloHostBuilder
{
    public static OrleansSiloHostBuilder CreateSiloHost(string clusterId, string serviceId, string? title = null,
        string[]? args = null, int gatewayPort = 12000, int siloPort = 13000, int dashboardPort = 5002,
        int apiPort = 5001, int healthCheckPort = 5081)
    {
        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        return new OrleansSiloHostBuilder(
            clusterId,
            serviceId,
            title ?? entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Orleans Host Default Name",
            args ?? Array.Empty<string>(),
            entryAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version ?? "0.1.0",
            gatewayPort: gatewayPort,
            siloPort: siloPort,
            dashboardPort: dashboardPort,
            apiPort: apiPort,
            healthCheckPort: healthCheckPort
        );
    }

    public static OrleansSiloHostBuilder CreateBuilder(string[]? args, int gatewayPort = 12000, int siloPort = 13000,
        int dashboardPort = 5002, int apiPort = 5001, int healthCheckPort = 5081)
    {
        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? throw new InvalidOperationException("Entry assembly not available.");

        return CreateSiloHost(
            entryAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company.ToLower() ??
            throw new InvalidOperationException(
                "[AssemblyCompanyAttribute] is missing and cannot generate orleans cluster id"),
            entryAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product.ToLower() ??
            throw new InvalidOperationException(
                "[AssemblyProductAttribute] is missing and cannot generate orleans service id"),
            args: args,
            gatewayPort: gatewayPort,
            siloPort: siloPort,
            dashboardPort: dashboardPort,
            apiPort: apiPort,
            healthCheckPort: healthCheckPort
        );
    }
}