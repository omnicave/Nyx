using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli.CommandHandlers;

public class HostResolvedMethodInfoCommandHandler<T> : BaseHostResolvedCommandHandler where T : notnull
{
    private readonly MethodInfo _methodInfo;
    private readonly HandlerDescriptor _handlerDescriptor;
    private readonly ParameterInfo[] _parameters;

    public HostResolvedMethodInfoCommandHandler(MethodInfo methodInfo)
    {
        _methodInfo = methodInfo;
        _handlerDescriptor = HandlerDescriptor.FromMethodInfo(_methodInfo);
        _parameters = _methodInfo.GetParameters();
    }

    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var host = context.BindingContext.GetRequiredService<IHost>();
        var rootServiceProvider = host.Services;
        var scope = rootServiceProvider.CreateAsyncScope();
        var scopedServiceProvider = scope.ServiceProvider;
        
        var handler = scopedServiceProvider.GetRequiredService<T>();

        var result = _methodInfo.Invoke(handler, BuildParameterValueList(_parameters, host, scopedServiceProvider, context) );
        
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