using Microsoft.Extensions.Hosting;

namespace Nyx.Cli;

public interface ICliHostBuilderFactory
{
    IHostBuilder CreateHostBuilder(IInvocationContext invocationContext);
}