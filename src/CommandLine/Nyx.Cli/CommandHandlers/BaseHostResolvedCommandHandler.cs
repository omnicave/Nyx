using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli.CommandBuilders;
using Nyx.Cli.Internal;

namespace Nyx.Cli.CommandHandlers;

public abstract class BaseHostResolvedCommandHandler : ICommandHandler
{
    protected object?[] BuildParameterValueList(ParameterInfo[] parameters, IHost host, IServiceProvider scopedServiceProvider, InvocationContext context)
    {
        var arguments = new Dictionary<string, object?>();

        foreach (var x in context.ParseResult.CommandResult.Children)
        {
            var y = x switch
            {
                ArgumentResult argumentResult => (x.Symbol.Name, value: x.GetValueForArgument(argumentResult.Argument)),
                OptionResult optionResult => (x.Symbol.Name, value: optionResult.GetValueOrDefault()),
                _ => (string.Empty, value: null)
            };
            
            if (y.value != null)
                arguments.Add(y.Name.ToLower(), y.Item2);
        }

        var parameterBuilder = new CliParameterBuilder();

        return parameters.Select(p =>
        {
            if (parameterBuilder.IsValidOptionOrArgumentParameter(p.ParameterType))
            {
                var key = p.Name!.ToLower();
                if (arguments.ContainsKey(key))
                    return arguments[key];
                else
                {
                    if (!p.IsOptional)
                    {
                        throw new InvalidOperationException(
                            "Cannot build parameter list for method because matching argument/option not supplied.");
                    }

                    return p.DefaultValue;
                }
            }
            else
            {
                return scopedServiceProvider.GetRequiredService(p.ParameterType);
            }

        }).ToArray();
    } 
    public int Invoke(InvocationContext context) => InvokeAsync(context).GetAwaiter().GetResult();
    public abstract Task<int> InvokeAsync(InvocationContext context);
}