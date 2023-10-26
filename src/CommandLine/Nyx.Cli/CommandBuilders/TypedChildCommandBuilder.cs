using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli.CommandHandlers;
using Nyx.Cli.Internal;

namespace Nyx.Cli.CommandBuilders;

internal class TypedChildCommandBuilder<TCliCommand> : BaseCommandBuilder, ICommandBuilder where TCliCommand : notnull
{
    private readonly Command _rootCommand;
    
    private readonly string _commandName;
    private readonly string? _commandAlias;
    private readonly string _description;
    private readonly List<(MethodInfo Info, CliSubCommandAttribute SubCommandAttribute, DescriptionAttribute Description, CliHostBuilderFactoryAttribute? HostBuilderFactoryAttribute)> _subCommands;
    private readonly MethodInfo? _commandExecuteMethodInfo;
    private readonly ICliHostBuilderFactory? _commandHostBuilderFactory;
    private bool SingleCommandMode => !_subCommands.Any() && _commandExecuteMethodInfo != null;
        
    public TypedChildCommandBuilder(Command rootCommand, ICliHostBuilderFactory builderProvidedHostFactory)
    {
        _rootCommand = rootCommand;
        var typeInfo = typeof(TCliCommand).GetTypeInfo();
            
        var cliCommandAttribute = typeInfo.GetCustomAttribute<CliCommandAttribute>();
        var descriptionAttribute = typeInfo.GetCustomAttribute<DescriptionAttribute>();

        var hostBuilderFactory = typeInfo.GetCustomAttribute<CliHostBuilderFactoryAttribute>();
        _commandHostBuilderFactory = hostBuilderFactory?.Instance ?? builderProvidedHostFactory;

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
                    Description: method.GetCustomAttribute<DescriptionAttribute>() ?? DescriptionAttribute.Default,
                    SubCommandHostFactory: method.GetCustomAttribute<CliHostBuilderFactoryAttribute>()
                    )
            )
            .Where(
                x => !ReferenceEquals(CliSubCommandAttribute.Default, x.SubCommandAttribute)
            )
            .ToList();

        var executeMethod = methods.FirstOrDefault(x => x.Name.Equals("Execute", StringComparison.OrdinalIgnoreCase));

        if (executeMethod != null) 
            _commandExecuteMethodInfo = executeMethod;

        if (executeMethod == null && _subCommands.Count == 0) 
            throw new InvalidOperationException($"Command '{typeof(TCliCommand).Name}' doesn't have any subcommands or an Execute method");
        
        
    }

    private void PopulateCommandFromMethodInfo(MethodInfo mi, Command command)
    {
        command.Handler = SetupCommandHandlerFromMethodInfo(mi);

        // build options
        var descriptor = HandlerDescriptor.FromMethodInfo(mi);
        var parameters = mi.GetParameters();

        PopulateCommandArgumentsAndOptions(command, _rootCommand, parameters, descriptor);
    }

    private ICommandHandler SetupCommandHandlerFromMethodInfo(MethodInfo mi)
    {
        return new HostResolvedMethodInfoCommandHandler<TCliCommand>(mi);
    }

    public Command Build()
    {
        Func<IInvocationContext,IHostBuilder>? act = _commandHostBuilderFactory != null ? (ctx => _commandHostBuilderFactory.CreateHostBuilder(ctx)) : null;
        var parentCommand = new NyxSystemConsoleCommand<TCliCommand>(
            _commandName,
            _description,
            act
        );

        if (_commandAlias != null) 
            parentCommand.AddAlias(_commandAlias);

        if (_commandExecuteMethodInfo != null)
        {
            PopulateCommandFromMethodInfo(_commandExecuteMethodInfo, parentCommand);
        }

        foreach (var method in _subCommands)
        {
            Func<IInvocationContext, IHostBuilder>? subHostBuilderFactory = null;
            if (method.HostBuilderFactoryAttribute != null)
            {
                subHostBuilderFactory = ctx => method.HostBuilderFactoryAttribute.Instance.CreateHostBuilder(ctx);
            }
            else if (_commandHostBuilderFactory != null)
            {
                subHostBuilderFactory = ctx => _commandHostBuilderFactory.CreateHostBuilder(ctx);
            }
            
            var subCommand = new NyxSystemConsoleCommand<TCliCommand>(method.SubCommandAttribute.Name, method.Description.Description, subHostBuilderFactory);
            if (method.SubCommandAttribute.HasAlias)
                subCommand.AddAlias(method.SubCommandAttribute.Alias);
            PopulateCommandFromMethodInfo(method.Info, subCommand);
            parentCommand.AddCommand(subCommand);
        }
        
        return parentCommand;
    }
}