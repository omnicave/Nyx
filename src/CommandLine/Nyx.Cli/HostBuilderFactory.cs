using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nyx.Cli;

internal class HostBuilderFactory
{
    public static IHostBuilder CreateHostBuilder() => CreateHostBuilder(new string[] { });

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                config.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddEnvironmentVariables();
                builder.AddCommandLine(args);
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
                logging.Configure(options =>
                {
                    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                      | ActivityTrackingOptions.TraceId
                                                      | ActivityTrackingOptions.ParentId;
                });
                logging.AddFilter("Microsoft", LogLevel.Error);
                logging.AddFilter("System", LogLevel.Error);
            });
}