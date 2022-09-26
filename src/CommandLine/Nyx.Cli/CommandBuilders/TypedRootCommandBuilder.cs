using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;
using Nyx.Cli.CommandHandlers;

namespace Nyx.Cli.CommandBuilders;

internal class TypedRootCommandBuilder<T> : BaseCommandBuilder, IRootCommandBuilder 
    where T : notnull
{
    private readonly string _name;
    private readonly MethodInfo[] _methods;

    public TypedRootCommandBuilder(string name)
    {
        _methods = typeof(T).GetTypeInfo().GetMethods();
        _name = name;
    }
    
    public Command Build()
    {        
        var executeMethod = _methods.FirstOrDefault(x => x.Name.Equals("Execute", StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException();
        
        var rootCommand = new Command(_name)
        {
            Handler = new HostResolvedMethodInfoCommandHandler<T>(executeMethod)
        };

        PopulateCommandArgumentsAndOptions(rootCommand, null, executeMethod.GetParameters(), HandlerDescriptor.FromMethodInfo(executeMethod));

        return rootCommand;
    }
}