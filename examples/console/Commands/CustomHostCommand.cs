using Microsoft.Extensions.Hosting;
using Nyx.Cli;

namespace Nyx.Examples.Console.Commands;

[CliCommand("start-host-1")]
[CliHostBuilderFactory(typeof(Host1HostFactory))]
public class CustomHostCommand
{
    public async Task Execute(IHost host)
    {
        System.Console.WriteLine("hello");
    }
}