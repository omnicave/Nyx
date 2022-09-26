using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Nyx.Cli.Tests.CommandLineBuilder;

public class GenericTests
{    
    [Fact]
    public async Task ExitCodeFromDelegateRootCommand()
    {
        var builder = CommandLineHostBuilder.Create(new string[] { })
            .WithRootCommandHandler((IInvocationContext ctx) => ctx.SetExitCode(5));

        var host = builder.Build();

        await host.StartAsync();

        Environment.ExitCode.Should().Be(5);
    }

    private class CommandWithReturnValue
    {
        public Task<int> Execute()
        {
            return Task.FromResult(5);
        }
    }
    
    private class CommandWithInvocationContext
    {
        public Task Execute(IInvocationContext context)
        {
            context.SetExitCode(6);
            return Task.CompletedTask;
        }
    }
    
    [Fact]
    public async Task ExitCodeFromTypedRootCommand_WithIntegerReturnValue()
    {

        var builder = CommandLineHostBuilder.Create(new string[] { })
            .WithRootCommandHandler<CommandWithReturnValue>();

        var host = builder.Build();

        await host.StartAsync();

        Environment.ExitCode.Should().Be(5);
    }
    
    [Fact]
    public async Task ExitCodeFromTypedRootCommand_WithIntegerInvocationContext()
    {

        var builder = CommandLineHostBuilder.Create(new string[] { })
            .WithRootCommandHandler<CommandWithInvocationContext>();

        var host = builder.Build();

        await host.StartAsync();

        Environment.ExitCode.Should().Be(6);
    }
}