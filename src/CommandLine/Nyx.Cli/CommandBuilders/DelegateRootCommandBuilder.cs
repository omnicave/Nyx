using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using Nyx.Cli.CommandHandlers;

namespace Nyx.Cli.CommandBuilders;

internal class DelegateRootCommandBuilder : BaseCommandBuilder, IRootCommandBuilder
{
    private readonly Delegate _delegate;
    private readonly string _name;

    public DelegateRootCommandBuilder(Delegate @delegate, string name)
    {
        _delegate = @delegate;
        _name = name;
    }
    
    public Command Build()
    {
        var descriptor = HandlerDescriptor.FromDelegate(_delegate);
        var command = new Command(_name)
        {
            Handler = new HostResolvedDelegateCommandHandler(_delegate, descriptor)
        };

        PopulateCommandArgumentsAndOptions(command, Enumerable.Empty<Option>(), _delegate.Method.GetParameters(), descriptor );

        return command;
    }
}