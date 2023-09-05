using System;
using System.CommandLine;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli.Internal;

internal class NyxSystemConsoleCommand<T> : Command, INyxSystemConsoleCommand
{
    public NyxSystemConsoleCommand(
        string name, 
        string? description = null, 
        Func<IInvocationContext, IHostBuilder>? hostBuilderFactory = null
        ) : base(name, description)
    {
        HostBuilderFactory = hostBuilderFactory ?? CommandLineHostBuilder.DefaultHostBuilderFactory;
    }

    public Func<IInvocationContext, IHostBuilder> HostBuilderFactory { get; }
    
    public Type GetCommandTypeAndMethodInfo() => typeof(T);
}

internal interface INyxSystemConsoleCommand
{
    Type GetCommandTypeAndMethodInfo();
    Func<IInvocationContext, IHostBuilder> HostBuilderFactory { get; }
}