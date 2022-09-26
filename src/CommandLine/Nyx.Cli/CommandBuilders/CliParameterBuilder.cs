using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nyx.Cli.CommandBuilders;

internal class CliParameterBuilder
{
    private Option BuildDateTimeOption(string name, ParameterInfo p, string description)
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

    public (bool isArgument, Symbol symbol, bool isGlobalOption) BuildArgumentOrOption(string name, TypeInfo type, ParameterInfo parameterInfo, IEnumerable<Option> globalOptions)
    {
        bool allowMultiple = false;

        if (type != typeof(string))
        {
            if (type.IsArray)
            {
                //type = type.GetElementType()?.GetTypeInfo() ?? throw new InvalidOperationException("Array error.");
                allowMultiple = true;
            }
            else if (ImplementsEnumerableT(type))
            {
                // var enumerableContract = type.GetInterfaces()
                //                              .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                //                          ?? throw new InvalidOperationException();
                //type = enumerableContract.GetGenericArguments().FirstOrDefault()?.GetTypeInfo() ?? throw new InvalidOperationException();
        
                allowMultiple = true;
            }    
        }
        
        
        var desc = (parameterInfo.GetCustomAttribute<DescriptionAttribute>() ?? DescriptionAttribute.Default)
            .Description;
        
        if (type == typeof(bool))
        {
            if (allowMultiple) throw new InvalidOperationException();
            
            return (
                false, 
                new Option<bool>(
                    $"--{name}",
                    desc
                ),
                false);
        }

        // determine if argument is an option, or a required argument
        var parameterCustomAttributes = parameterInfo.GetCustomAttributes().ToArray();

#pragma warning disable CS0612
        var isCliArgument = parameterCustomAttributes.OfType<CliSubCommandArgumentAttribute>().Any() ||
                            parameterCustomAttributes.OfType<CliArgumentAttribute>().Any();
#pragma warning restore CS0612
        var isCliOption = parameterCustomAttributes.OfType<CliOptionAttribute>().Any();
        
        if (!isCliArgument && !isCliOption)
        {
            if (!parameterInfo.IsOptional)
            {
                isCliArgument = true;
                isCliOption = false;
            }
            else
            {
                isCliOption = true;
                isCliArgument = false;
            }
        }

        // sanity check
        if (isCliArgument && isCliOption)
            throw new InvalidOperationException(
                "Cannot have a Option and Argument attribute at the same time on a parameter");

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

        // we reached this point, then we are dealing with an option
        if (type == typeof(DateTime))
        {
            return (
                false,
                EnableAllowMultipleInstances(
                    BuildDateTimeOption(
                        name,
                        parameterInfo,
                        desc),
                    allowMultiple
                ),
                false
            );
        }

        // we always assume global options are stored in the root command
        var globalOption = globalOptions.FirstOrDefault(opt => opt.Name == name);
        return (
            false,
            globalOption ?? EnableAllowMultipleInstances(BuildGenericOptionFromType(type, name, desc), allowMultiple),
            globalOption != null
        );
    }

    public bool IsValidOptionOrArgumentParameter(Type type) => type.IsValueType || type.IsArray || ImplementsEnumerableT(type) || IsFileOrDirectoryType(type);

    private bool ImplementsEnumerableT(Type type) =>
        type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    private bool IsFileOrDirectoryType(Type type) =>
        type == typeof(FileInfo) || type == typeof(DirectoryInfo) || type == typeof(File) || type == typeof(Directory);

    private static Option EnableAllowMultipleInstances(Option o, bool value = true)
    {
        o.AllowMultipleArgumentsPerToken = value;
        return o;
    }
}