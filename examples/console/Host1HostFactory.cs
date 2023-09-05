using Microsoft.Extensions.Hosting;
using Nyx.Cli;

namespace Nyx.Examples.Console;

public class Host1HostFactory : ICliHostBuilderFactory
{
    public IHostBuilder CreateHostBuilder(IInvocationContext invocationContext) => new HostBuilder();
}