using Nyx.Cli;
using Nyx.Hosting;
using Nyx.Orleans.Serialization;
using Orleans;
using Orleans.Configuration;

namespace Nyx.Orleans.Host;

public class OrleansClientHostBuilder : BaseHostBuilder
{
    private readonly string _clientName;
    private readonly string _clusterId;
    private readonly string _serviceId;
    private readonly string[]? _args;

    private bool _isDaemonOrleansClient = false;

    public OrleansClientHostBuilder(string clientName, string clusterId, string serviceId, string[]? args = null)
    {
        _clientName = clientName;
        _clusterId = clusterId;
        _serviceId = serviceId;
        _args = args;
    }

    internal readonly List<Action<IClientBuilder>> ClientExtraConfiguration = new();
    internal readonly List<Action<ICommandLineHostBuilder>> CliExtraConfiguration = new();

    public override IHost Build()
    {
        var x = ClientExtraConfiguration.Concat(new Action<IClientBuilder>[]
            {
                builder => builder.Configure<SerializationProviderOptions>(options =>
                    {
                        options.FallbackSerializationProvider = typeof(NewtonsoftJsonExternalSerializer);
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = _clusterId;
                        options.ServiceId = _serviceId;
                    })
            })
            .ToList();

        var cli = CommandLineHostBuilder.Create(_clientName, _args ?? Array.Empty<string>());
        cli.AddOrleansClusterClient(x);

        foreach (var item in CliExtraConfiguration)
        {
            item(cli);
        }

        if (_isDaemonOrleansClient)
        {
            cli.WithRootCommandHandler(async (IHost host) =>
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
            });
        }

        return cli.Build();
    }
}