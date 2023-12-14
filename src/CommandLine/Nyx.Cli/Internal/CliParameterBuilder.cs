using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nyx.Cli.Internal;

internal class CliParameterBuilder
{
    private Option BuildDateTimeOption(string name, string description)
    {
        return new Option<DateTime>(
            $"--{name}",
            result => DateTime.Parse(result.ToString(), CultureInfo.CurrentCulture),
            false,
            description
        );
    }
    
    private Argument BuildDateTimeArgument(string name, ParameterInfo p, string description) =>
        new Argument<DateTime>(
            name,
            result => DateTime.Parse(result.ToString(), CultureInfo.CurrentCulture),
            false,
            description
        );

    private Argument BuildGenericArgumentFromType(Type type, string name, string description)
    {
        var argType = typeof(Argument<>)
            .MakeGenericType(type);

        var result = (Argument?)Activator.CreateInstance(
            argType,
            name,
            description
        );

        return result ??
               throw new InvalidOperationException($"Cannot create a generic instance of Argument for {type}.");
    }

    private Option BuildGenericOptionFromType(Type type, string name, string description)
    {
        var optionType = typeof(Option<>)
            .MakeGenericType(type);
                
        var result = (Option?)Activator.CreateInstance(
            optionType,
            $"--{name}",
            description
        );
        
        return result ??
               throw new InvalidOperationException($"Cannot create a generic instance of Option for {type}.");
    }

    private bool AllowsMultipleValues(TypeInfo type)
    {
        if (type != typeof(string))
        {
            if (type.IsArray)
            {
                return true;
            }
            else if (ImplementsEnumerableT(type))
            {
                return true;
            }    
        }

        return false;
    }
    
    public Option BuildCommandLineOption(string name, TypeInfo type, string? description = null)
    {
        var allowMultiple = AllowsMultipleValues(type);
        
        if (type == typeof(bool))
        {
            if (allowMultiple) 
                throw new InvalidOperationException();
            
            return new Option<bool>(
                    $"--{name}",
                    description ?? string.Empty
                );
        }
        
        if (type == typeof(DateTime))
        {
            return EnableAllowMultipleInstancesOnOption(
                BuildDateTimeOption(
                    name,
                    description ?? string.Empty),
                allowMultiple
            );
        }

        return EnableAllowMultipleInstancesOnOption(BuildGenericOptionFromType(type, name, description ?? string.Empty), allowMultiple);
    }

    protected bool DetermineHeuristicallyIfParameterIsACliArgument(ParameterInfo parameterInfo)
    {
        if (parameterInfo.ParameterType == typeof(bool))
            return false;
        
        if (parameterInfo.ParameterType.IsArray)
        {
            // if parameter is an array then for sure it's an option, since it needs to be specified multiple times
            return false;
        }

        if (parameterInfo.IsOptional)
            // parameter is optional, so yeah it's an option
            return false;
        
        // parameter is not optional, does it have a default tho?
        if (parameterInfo.HasDefaultValue)
            return false;
            
        return true;

        
    } 

    public (bool isArgument, Symbol symbol, bool isGlobalOption) BuildArgumentOrOption(string name, TypeInfo type, ParameterInfo parameterInfo, IEnumerable<Option> globalOptions)
    {
        var allowMultiple = AllowsMultipleValues(type);
        var desc = (parameterInfo.GetCustomAttribute<DescriptionAttribute>() ?? DescriptionAttribute.Default)
            .Description;
        
        if (type == typeof(bool))
        {
            // short circuit the flow and return immediately with a command line option
            if (allowMultiple) 
                throw new InvalidOperationException();
            return (false, BuildCommandLineOption(name, type, desc), false);
        }

        // determine if argument is an option, or a required argument
        var parameterCustomAttributes = parameterInfo.GetCustomAttributes().ToArray();

#pragma warning disable CS0612
        var isCliArgument = parameterCustomAttributes.OfType<CliSubCommandArgumentAttribute>().Any() ||
                            parameterCustomAttributes.OfType<CliArgumentAttribute>().Any();
#pragma warning restore CS0612
        var isCliOption = parameterCustomAttributes.OfType<CliOptionAttribute>().Any();
        
        // sanity check
        if (isCliArgument && isCliOption)
            throw new InvalidOperationException(
                "Cannot have a Option and Argument attribute at the same time on a parameter");

        
        if (!isCliArgument && !isCliOption)
        {
            // attributes where not specified, let's do some heuristics
            isCliArgument = DetermineHeuristicallyIfParameterIsACliArgument(parameterInfo);
        }
        
        if (isCliArgument)
        {
            if (allowMultiple) 
                throw new InvalidOperationException($"Parameter {name} on [{parameterInfo.Member.DeclaringType?.Name ?? "-"}::{parameterInfo.Member.Name}] is marked as an argument but accepts multiple values (Possible causes: non optional array argument?).");
            
            if (type == typeof(DateTime))
            {
                return (
                    true,
                    BuildDateTimeArgument(name, parameterInfo, desc),
                    false
                );
            }

            return (
                true,
                BuildGenericArgumentFromType(type, name, desc),
                false
            );
        }
        
        // we need to handle the scenario where the command is referencing an a global argument, either at the root
        // command, or at the parent.
        // we always assume global options are stored in the root command
        var opt = globalOptions.FirstOrDefault(opt => opt.Name == name);

        if (opt != null)
            return (
                false,
                opt,
                true
            );
        
        // argument doesn't reference a global option, let's build it
        opt = BuildCommandLineOption(name, type, desc);
        return (
            false,
            opt,
            false
        );

    }

    public bool IsValidOptionOrArgumentParameter(Type type) => type.IsValueType || type.IsArray || ImplementsEnumerableT(type) || IsFileOrDirectoryType(type);

    private bool ImplementsEnumerableT(Type type) =>
        type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    private bool IsFileOrDirectoryType(Type type) =>
        type == typeof(FileInfo) || type == typeof(DirectoryInfo) || type == typeof(File) || type == typeof(Directory);

    private static Option EnableAllowMultipleInstancesOnOption(Option o, bool value = true)
    {
        o.AllowMultipleArgumentsPerToken = value;
        return o;
    }
}