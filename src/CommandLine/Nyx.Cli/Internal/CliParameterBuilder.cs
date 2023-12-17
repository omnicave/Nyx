using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Nyx.Cli.Internal;

internal class CliParameterBuilder
{
    private Argument BuildArgumentFromType(Type type, string name, string description)
    {
        var mi = GetType()
            .GetMethod(
                nameof(BuildArgument), 
                BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance
            )?
            .MakeGenericMethod(type);

        if (mi == null)
            throw new InvalidOperationException("Could not register typed command due - invalid method info");
        
        return (Argument)mi.Invoke(this, new object?[]
        {
            name,
            description
        } )!;
    }

    private Argument BuildArgument<T>(string name, string description)
    {
        if (typeof(T) == typeof(DateTime))
        {
            return new Argument<DateTime>(
                name,
                result => DateTime.Parse(result.ToString(), CultureInfo.CurrentCulture),
                description: description
            );
        }
        
        return new Argument<T>(
            name,
            description: description
        );


    }

    private Option BuildOption<T>(string name, string description, string? alias)
    {
        var aliases = (string[])new[]
        {
            $"--{name}"
        }.Concat(
            string.IsNullOrWhiteSpace(alias) ? Array.Empty<string>() : new[] { $"-{alias}" }
        ).ToArray();
        
        // handle the specific cases
        if (typeof(T) == typeof(DateTime))
        {
            return new Option<DateTime>(
                aliases,
                result => DateTime.Parse(result.ToString(), CultureInfo.CurrentCulture),
                false,
                description
            );
        }
        
        if (typeof(T) == typeof(bool))
        {
            return new Option<bool>(
                aliases,
                description ?? string.Empty
            );
        }

        return new Option<T>(
            aliases,
            description: description
        );
    }
    

    private Option BuildOptionFromType(Type type, string name, string description, string? alias)
    {
        var mi = GetType()
            .GetMethod(
                nameof(BuildOption), 
                BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance
            )?
            .MakeGenericMethod(type);

        if (mi == null)
            throw new InvalidOperationException("Could not register typed command due - invalid method info");
        
        return (Option)mi.Invoke(this, new object?[]
        {
            name,
            description,
            alias
        } )!;
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

    public Argument BuildCommandLineArgument(string name, TypeInfo type, string? description = null)
    {
        if (type == typeof(DateTime))
        {
            return BuildArgument<DateTime>(name, description ?? string.Empty);
        }

        return BuildArgumentFromType(type, name, description ?? string.Empty);
    }

    public Option BuildCommandLineOption(string name, TypeInfo type, string? description = null, string? alias = null)
    {
        var allowMultiple = AllowsMultipleValues(type);

        if (type == typeof(bool))
        {
            if (allowMultiple)
                throw new InvalidOperationException();

            return BuildOption<bool>(name, description ?? string.Empty, alias);
        }

        if (type == typeof(DateTime))
        {
            return EnableAllowMultipleInstancesOnOption(
                BuildOption<DateTime>(
                    name,
                    description ?? string.Empty, 
                    alias
                    ),
                allowMultiple
            );
        }

        var opt = EnableAllowMultipleInstancesOnOption(
            BuildOptionFromType(type, name, description ?? string.Empty, alias),
            allowMultiple
        );

        return opt;
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

    public (bool isArgument, Symbol symbol, bool isGlobalOption) BuildArgumentOrOptionFromParameterInfo(
        string name, 
        TypeInfo type,
        ParameterInfo parameterInfo, 
        IEnumerable<Option> globalOptions)
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
        
        var isCliArgument = parameterCustomAttributes.OfType<CliArgumentAttribute>().Any();
        var isCliOption = parameterCustomAttributes.OfType<CliOptionAttribute>().Any();

        // sanity check
        if (isCliArgument && isCliOption)
            throw new InvalidOperationException($"Cannot have both {nameof(CliArgumentAttribute)} and {nameof(CliOptionAttribute)} on a parameter.");
        
        if (!isCliArgument && !isCliOption)
        {
            // attributes where not specified, let's do some heuristics
            isCliArgument = DetermineHeuristicallyIfParameterIsACliArgument(parameterInfo);
        }

        if (isCliArgument)
        {
            if (allowMultiple)
                throw new InvalidOperationException(
                    $"Parameter {name} on [{parameterInfo.Member.DeclaringType?.Name ?? "-"}::{parameterInfo.Member.Name}] is marked as an argument but accepts multiple values (Possible causes: non optional array argument?).");

            return (true,
                    BuildCommandLineArgument(name, type, description: desc),
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

    public bool IsValidOptionOrArgumentParameter(Type type) => type.IsValueType || type.IsArray ||
                                                               ImplementsEnumerableT(type) ||
                                                               IsFileOrDirectoryType(type);

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