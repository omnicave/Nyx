using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli;

public class MethodInfoBasedCommandBuilder<TCliCommand> : ICommandBuilder
    where TCliCommand : ICliCommand
{
    private readonly Command _rootCommand;
    
    private readonly string _commandName;
    private readonly string? _commandAlias;
    private readonly string _description;
    private readonly List<(MethodInfo Info, CliSubCommandAttribute SubCommandAttribute, DescriptionAttribute Description)> _subCommands;
    private readonly MethodInfo? _singleCommandMethodInfo;
    private bool SingleCommandMode => !_subCommands.Any() && _singleCommandMethodInfo != null;
        
    public MethodInfoBasedCommandBuilder(Command rootCommand)
    {
        _rootCommand = rootCommand;
        var typeInfo = typeof(TCliCommand).GetTypeInfo();
            
        var cliCommandAttribute = typeInfo.GetCustomAttribute<CliCommandAttribute>();
        var descriptionAttribute = typeInfo.GetCustomAttribute<DescriptionAttribute>();

        if (cliCommandAttribute == null)
        {
            var name = typeInfo.Name.ToLower();
            name = name.EndsWith("command") ? name[..^"command".Length] : name;
            _commandName = name;
            _commandAlias = null;
        }
        else
        {
            if (cliCommandAttribute.HasAlias)
                _commandAlias = cliCommandAttribute.Alias;
            _commandName = cliCommandAttribute.Name;
        }
            
        _description = descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;

        var methods = typeInfo.GetMethods();
        _subCommands = methods
            .Select(
                method => (
                    Info: method,
                    SubCommandAttribute: method.GetCustomAttribute<CliSubCommandAttribute>() ??
                                         CliSubCommandAttribute.Default,
                    Description: method.GetCustomAttribute<DescriptionAttribute>() ?? DescriptionAttribute.Default)
            )
            .Where(
                x => !ReferenceEquals(CliSubCommandAttribute.Default, x.SubCommandAttribute)
            )
            .ToList();

        if (_subCommands.Any()) 
            return;
            
        // SINGLE COMMAND MODE!
        // command doesn't have any subcommands
        var executeMethod =
            methods.FirstOrDefault(x => x.Name.Equals("Execute", StringComparison.OrdinalIgnoreCase));

        if (executeMethod == null)
            throw new InvalidOperationException("Command doesn't have any subcommands or an Execute method");

        _singleCommandMethodInfo = executeMethod;
    }

    private Option BuildDateTimeOption(string name, ParameterInfo p)
    {
        var description =
            (p.GetCustomAttribute<DescriptionAttribute>() ??
             DescriptionAttribute.Default).Description;

        var opt = new Option<DateTime>(
            $"--{name}",
            result => DateTime.Parse(result.ToString(), CultureInfo.CurrentCulture),
            false,
            description
        );

        return opt;
    }

    private void PopulateCommandFromMethodInfo(MethodInfo mi, Command command)
    {
        command.Handler = SetupCommandHandlerFromMethodInfo(mi);

        // build options
        var descriptor = HandlerDescriptor.FromMethodInfo(mi);

        var parameterInfoLookup = mi.GetParameters()
            .ToDictionary<ParameterInfo, string>(x => x.Name!);

        descriptor.ParameterDescriptors
            .Select(
                x =>
                {
                    (bool isArgument, Symbol? symbol, bool isGlobalOption) result;
                    if (x.ValueType == typeof(bool))
                    {
                        var symbol = (Symbol)new Option<bool>(
                            $"--{x.ValueName}",
                            (parameterInfoLookup[x.ValueName].GetCustomAttribute<DescriptionAttribute>() ??
                             DescriptionAttribute.Default).Description
                        );

                        result = (false, symbol, false);
                    }
                    else
                    {
                        var cliSubCommandArgument = parameterInfoLookup[x.ValueName]
                            .GetCustomAttribute<CliSubCommandArgumentAttribute>();

                        if (cliSubCommandArgument != null)
                        {
                            var argType = typeof(Argument<>)
                                .MakeGenericType(x.ValueType);

                            var symbol = (Argument?)Activator.CreateInstance(
                                argType,
                                x.ValueName,
                                (parameterInfoLookup[x.ValueName]
                                     .GetCustomAttribute<DescriptionAttribute>() ??
                                 DescriptionAttribute.Default).Description
                            );

                            result = (true, symbol, false);
                        }
                        else
                        {
                            if (x.ValueType == typeof(DateTime))
                            {
                                result = (
                                    false,
                                    BuildDateTimeOption(x.ValueName, parameterInfoLookup[x.ValueName]),
                                    false
                                );
                            }
                            else
                            {
                                // we always assume global options are stored in the root command
                                var globalOption = _rootCommand.Options.FirstOrDefault(opt => opt.Name == x.ValueName);
                                
                                if (globalOption == null)
                                {
                                    var optionType = typeof(Option<>)
                                        .MakeGenericType(x.ValueType);

                                    var description = (
                                            parameterInfoLookup[x.ValueName]
                                                .GetCustomAttribute<DescriptionAttribute>() ??
                                            DescriptionAttribute.Default)
                                        .Description;
                                    var symbol = (Option?)Activator.CreateInstance(
                                        optionType,
                                        $"--{x.ValueName}",
                                        description
                                    );

                                    result = (false, symbol, false);
                                }
                                else
                                {
                                    result = (false, globalOption, true);
                                }
                            }
                        }
                    }

                    if (result.symbol == null)
                        throw new InvalidOperationException();

                    return result;
                }
            )
            .ToList()
            .ForEach(tuple =>
            {
                var (isArgument, symbol, isGlobal) = tuple;

                if (!isArgument)
                {
                    if (!isGlobal)
                        command.AddOption((Option?)symbol ?? throw new InvalidOperationException());
                }
                else
                {
                    command.AddArgument((Argument?)symbol ?? throw new InvalidOperationException());
                }
            });
    }

    private ICommandHandler SetupCommandHandlerFromMethodInfo(MethodInfo mi)
    {
        return CommandHandler.Create(
            async (IHost host, ParseResult parseResult, InvocationContext context, IConsole console) =>
            {
                var instance = host.Services.GetRequiredService<TCliCommand>();

                var d = HandlerDescriptor.FromMethodInfo(mi, instance);
                var handler = d.GetCommandHandler();
                await handler.InvokeAsync(context);
                // var invoker = new ModelBindingCommandHandler(mi, descriptor, instance);
                // await invoker.InvokeAsync(context);
            });
    }

    public Command Build()
    {
        var parentCommand = new Command(
            _commandName,
            _description
        );

        if (_commandAlias != null) 
            parentCommand.AddAlias(_commandAlias);

        if (_singleCommandMethodInfo != null)
        {
            PopulateCommandFromMethodInfo(_singleCommandMethodInfo, parentCommand);
        }
        else
        {
            foreach (var method in _subCommands)
            {
                var subCommand = new Command(method.SubCommandAttribute.Name, method.Description.Description);

                if (method.SubCommandAttribute.HasAlias)
                    subCommand.AddAlias(method.SubCommandAttribute.Alias);
                
                PopulateCommandFromMethodInfo(method.Info, subCommand);

                // var descriptor = HandlerDescriptor.FromMethodInfo(method.Info);
                // subCommand.Handler = SetupCommandHandlerFromMethodInfo(method.Info);
                //
                // // build options
                // var parameterInfoLookup = method.Info.GetParameters()
                //     .ToDictionary<ParameterInfo, string>(x => x.Name!);
                //
                // descriptor.ParameterDescriptors
                //     .Select(
                //         x =>
                //         {
                //             (bool isArgument, Symbol? symbol) result;
                //             if (x.ValueType == typeof(bool))
                //             {
                //                 var symbol = (Symbol)new Option<bool>(
                //                     $"--{x.ValueName}",
                //                     (parameterInfoLookup[x.ValueName].GetCustomAttribute<DescriptionAttribute>() ??
                //                      DescriptionAttribute.Default).Description
                //                 );
                //
                //                 result = (false, symbol);
                //             }
                //             else
                //             {
                //                 var cliSubCommandArgument = parameterInfoLookup[x.ValueName]
                //                     .GetCustomAttribute<CliSubCommandArgumentAttribute>();
                //
                //                 if (cliSubCommandArgument != null)
                //                 {
                //                     var argType = typeof(Argument<>)
                //                         .MakeGenericType(x.ValueType);
                //
                //                     var symbol = (Argument?)Activator.CreateInstance(
                //                         argType,
                //                         x.ValueName,
                //                         (parameterInfoLookup[x.ValueName]
                //                              .GetCustomAttribute<DescriptionAttribute>() ??
                //                          DescriptionAttribute.Default).Description
                //                     );
                //
                //                     result = (true, symbol);
                //                 }
                //                 else
                //                 {
                //                     if (x.ValueType == typeof(DateTime))
                //                     {
                //                         result = (
                //                             false,
                //                             BuildDateTimeOption(x.ValueName, parameterInfoLookup[x.ValueName])
                //                         );
                //                     }
                //                     else
                //                     {
                //                         var optionType = typeof(Option<>)
                //                             .MakeGenericType(x.ValueType);
                //
                //                         var description =
                //                             (parameterInfoLookup[x.ValueName]
                //                                  .GetCustomAttribute<DescriptionAttribute>() ??
                //                              DescriptionAttribute.Default).Description;
                //                         var symbol = (Option?)Activator.CreateInstance(
                //                             optionType,
                //                             $"--{x.ValueName}",
                //                             description
                //                         );
                //
                //                         result = (false, symbol);
                //                     }
                //                 }
                //             }
                //
                //             if (result.symbol == null)
                //                 throw new InvalidOperationException();
                //
                //             return result;
                //         }
                //     )
                //     .ToList()
                //     .ForEach(tuple =>
                //     {
                //         var (isArgument, symbol) = tuple;
                //
                //         if (!isArgument)
                //             subCommand.AddOption((Option?)symbol ?? throw new InvalidOperationException());
                //         else
                //             subCommand.AddArgument((Argument?)symbol ?? throw new InvalidOperationException());
                //     });

                parentCommand.AddCommand(subCommand);
            }
        }

            
        return parentCommand;
    }
}