using System;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli.Internal;

public class DefaultCliHostBuilderFactory : ICliHostBuilderFactory
{
    public IHostBuilder CreateHostBuilder(IInvocationContext invocationContext) => CommandLineHostBuilder.DefaultHostBuilderFactory(invocationContext);
}

public class ActionBasedCliHostBuilderFactory : ICliHostBuilderFactory
{
    private readonly Func<IInvocationContext, IHostBuilder> _action;

    public ActionBasedCliHostBuilderFactory(Func<IInvocationContext, IHostBuilder> action)
    {
        _action = action;
    }
    
    public IHostBuilder CreateHostBuilder(IInvocationContext invocationContext)
    {
        return _action(invocationContext);
    }
}