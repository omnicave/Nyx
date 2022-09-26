using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;

namespace Nyx.Cli.CommandBuilders;

internal abstract class BaseCommandBuilder
{
    protected void PopulateCommandArgumentsAndOptions(Command command, Command? rootCommand, ParameterInfo[] parameters, HandlerDescriptor descriptor)
    {
        var cliParameterBuilder = new CliParameterBuilder();
        
        var parameterInfoLookup = parameters.ToDictionary<ParameterInfo, string>(x => x.Name!);

        descriptor.ParameterDescriptors
            .Where(x=> cliParameterBuilder.IsValidOptionOrArgumentParameter(x.ValueType) )
            .Select(parameterDescriptor => cliParameterBuilder.BuildArgumentOrOption(
                parameterDescriptor.ValueName, 
                parameterDescriptor.ValueType.GetTypeInfo(), 
                parameterInfoLookup[parameterDescriptor.ValueName], 
                rootCommand?.Options ?? Enumerable.Empty<Option>()
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