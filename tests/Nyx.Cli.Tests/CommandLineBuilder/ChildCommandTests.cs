using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Nyx.Cli.Tests.CommandLineBuilder;

public class ChildCommandTests
{    
    [CliCommand("sub")]
    public class Command
    {
        public Task<int> Execute()
        {
            return Task.FromResult(1);
        }
    }    
    
    public class CommandWithoutAttribute
    {
        public Task<int> Execute()
        {
            return Task.FromResult(1);
        }
    }
    
    [CliCommand("sub")]
    public class CommandWithoutExecute
    {
    }

    [Theory]
    [InlineData(typeof(Command), "sub", true )]
    [InlineData(typeof(CommandWithoutAttribute), nameof(CommandWithoutAttribute), true )]
    [InlineData(typeof(CommandWithoutExecute), "sub", false )]
    public async Task RegisterTypedCommand(Type t, string commandName, bool isSuccess)
    {
        var builder = CommandLineHostBuilder.Create(new string[] { })
            .RegisterCommand(t);

        var p = () => (CommandLineHost)builder.Build();

        if (!isSuccess)
        {
            p.Should().Throw<InvalidOperationException>();
        }
        else
        {
            var host = p.Should()
                .NotThrow().Subject;

            var command = host.Configuration.RootCommand.Subcommands
                .Should()
                .ContainSingle()
                .Subject;

            command.Name.Should().Be(commandName.ToLower());
        }
    }
}