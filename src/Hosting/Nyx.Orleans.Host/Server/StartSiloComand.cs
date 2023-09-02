using System.ComponentModel;
using Nyx.Cli;

namespace Nyx.Orleans.Host;

[CliCommand("start-silo")]
public class StartSiloComand
{
    public async Task<int> Execute(
        IHost host,
        [CliOption] int gatewayPort = 12000, 
        [CliOption] int siloPort = 13000, 
        [CliOption] int dashboardPort = 5002, 
        [CliOption] int apiPort = 5001, 
        [CliOption] int healthCheckPort = 5081
        )
    {
        var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    
        var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        applicationLifetime.ApplicationStopping
            .Register(obj =>
                {
                    var tcs = (TaskCompletionSource<object>)obj;
                    tcs.TrySetResult(null);
                },
                waitForStop);
        //
        await waitForStop.Task;

        return 0;
    }
}