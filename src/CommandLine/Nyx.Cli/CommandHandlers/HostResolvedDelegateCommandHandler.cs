using System;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli.CommandHandlers;

public class HostResolvedDelegateCommandHandler : BaseHostResolvedCommandHandler
{
    private readonly Delegate _handlerDelegate;
    private readonly HandlerDescriptor _handlerDescriptor;
    private readonly ParameterInfo[] _parameters;

    public HostResolvedDelegateCommandHandler(Delegate handlerDelegate, HandlerDescriptor handlerDescriptor)
    {
        _handlerDelegate = handlerDelegate;
        _handlerDescriptor = handlerDescriptor;
        _parameters = _handlerDelegate.Method.GetParameters();
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var host = context.BindingContext.GetRequiredService<IHost>();
        var rootServiceProvider = host.Services;
        var scope = rootServiceProvider.CreateAsyncScope();
        var scopedServiceProvider = scope.ServiceProvider;

        var result = _handlerDelegate.DynamicInvoke(BuildParameterValueList(_parameters, host, scopedServiceProvider, context));
        
        switch (result)
        {
            case Task<int> exitCodeTask:
                return await exitCodeTask;
            case Task task:
                await task;
                return context.ExitCode;
            case int exitCode:
                return exitCode;
            default:
                return context.ExitCode;
        }
    }
}