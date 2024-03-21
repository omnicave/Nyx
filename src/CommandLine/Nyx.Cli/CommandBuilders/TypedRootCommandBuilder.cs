using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli.CommandHandlers;
using Nyx.Cli.Internal;

namespace Nyx.Cli.CommandBuilders;

internal class TypedRootCommandBuilder<T> : BaseCommandBuilder, IRootCommandBuilder 
    where T : notnull
{
    private readonly string _name;
    private readonly MethodInfo[] _methods;
    private readonly ICliHostBuilderFactory? _commandHostBuilderFactory;

    public TypedRootCommandBuilder(string name, ICliHostBuilderFactory builderProvidedHostFactory)
    {
        _methods = typeof(T).GetTypeInfo().GetMethods();
        _name = name;
        
        var typeInfo = typeof(T).GetTypeInfo();
        var hostBuilderFactory = typeInfo.GetCustomAttribute<CliHostBuilderFactoryAttribute>();
        if (hostBuilderFactory != null)
            _commandHostBuilderFactory = hostBuilderFactory.Instance;
        _commandHostBuilderFactory ??= builderProvidedHostFactory;
    }
    
    public Command Build()
    {        
        Func<IInvocationContext,IHostBuilder>? act = _commandHostBuilderFactory != null ? (ctx => _commandHostBuilderFactory.CreateHostBuilder(ctx)) : null;
        var executeMethod = _methods.FirstOrDefault(x => x.Name.Equals("Execute", StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException();
        
        var rootCommand = new NyxSystemConsoleCommand<T>(_name, hostBuilderFactory: act)
        {
            Handler = new HostResolvedMethodInfoCommandHandler<T>(executeMethod)
        };

        PopulateCommandArgumentsAndOptions(rootCommand, Enumerable.Empty<Option>(), executeMethod.GetParameters(), HandlerDescriptor.FromMethodInfo(executeMethod));

        return rootCommand;
    }
}