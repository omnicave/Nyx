using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;
using Nyx.Cli.Internal;

namespace Nyx.Cli.CommandBuilders;

internal abstract class BaseCommandBuilder
{
    protected class BaseCommand<T> : Command
    {
        public BaseCommand(string name, string? description = null) : base(name, description)
        {
        }
    }
    
    protected void PopulateCommandArgumentsAndOptions(Command command, IEnumerable<Option> globalOptions, ParameterInfo[] parameters, HandlerDescriptor descriptor)
    {
        var cliParameterBuilder = new CliParameterBuilder();
        
        var parameterInfoLookup = parameters.ToDictionary<ParameterInfo, string>(x => x.Name!);

        descriptor.ParameterDescriptors
            .Where(x=> cliParameterBuilder.IsValidOptionOrArgumentParameter(x.ValueType) )
            .Select(parameterDescriptor => cliParameterBuilder.BuildArgumentOrOption(
                parameterDescriptor.ValueName, 
                parameterDescriptor.ValueType.GetTypeInfo(), 
                parameterInfoLookup[parameterDescriptor.ValueName], 
                globalOptions
                )
            )
            .ToList()
            .ForEach(tuple =>
            {
                var (isArgument, symbol, isGlobal) = tuple;

                if (!isArgument)
                {
                    if (!isGlobal)
                        command.AddOption((Option)symbol ?? throw new InvalidOperationException());
                }
                else
                {
                    command.AddArgument((Argument)symbol ?? throw new InvalidOperationException());
                }
            });
    }
}