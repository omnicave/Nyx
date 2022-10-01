using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Nyx.Cli.Tests.CommandLineBuilder.RootCommand;

public class ParameterParsingTests
{
    public ParameterParsingTests(ITestOutputHelper testOutputHelper)
    {
        Console.SetOut(new TestOutputHelperTextWriter(testOutputHelper));
        Console.SetError(new TestOutputHelperTextWriter(testOutputHelper));
    }
    
    [Fact]
    public async Task WithDelegateRootCommandHandler_ParseIntegerArgumentAndEvaluateService()
    {
        var called = false;
        
        await CommandLineHostBuilder.Create("2".Split(' '))
            .ConfigureServices((context, collection) =>
            {
                collection.AddScoped<IRandomTextService, RandomTextService>();
            })
            .WithRootCommandHandler((IRandomTextService randomTextService, [CliArgument] int max) =>
            {
                called = true;
                randomTextService.Should().NotBeNull().And.BeOfType<RandomTextService>();
                max.Should().Be(2);
            })
            .Build()
            .RunAsync();

        called.Should().BeTrue();
    }

    internal interface IRootCommandHandler
    {
        public Task<int> Execute(IRandomTextService randomTextService, int min, int max);
    }
    
    [Fact]
    public async Task WithTypedRootHandler_ParseIntegerArgumentsAndEvalServices()
    {
        var mock = new Mock<IRootCommandHandler>();
        mock.Setup(handler =>
                handler.Execute(
                    It.IsAny<IRandomTextService>(),
                    It.Is(1, EqualityComparer<int>.Default),
                    It.Is(5, EqualityComparer<int>.Default))
            )
            .Verifiable();
        
        await CommandLineHostBuilder.Create("1 5".Split(' '))
            .ConfigureServices((context, collection) =>
            {
                collection.AddScoped<IRandomTextService, RandomTextService>();
            })
            .WithRootCommandHandler(() => mock.Object)
            .Build()
            .RunAsync();

        mock.Verify();
    }
    
    [Fact]
    public async Task WithAsyncDelegateRootCommandHandler_NoArguments_WithExitCode()
    {
        var called = false;

        Task<int> RootCommandWithGenericTask()
        {
            called = true;
            return Task.FromResult(1);
        }
        
        await CommandLineHostBuilder.Create(Array.Empty<string>())
            .ConfigureServices((context, collection) =>
            {
                collection.AddScoped<IRandomTextService, RandomTextService>();
            })
            .WithRootCommandHandler(RootCommandWithGenericTask)
            .Build()
            .RunAsync();

        called.Should().BeTrue();
        Environment.ExitCode.Should().Be(1);
    }
    
    [Fact]
    public async Task WithAsyncDelegateRootCommandHandler_NoArguments()
    {
        var called = false;

        Task RootCommandWithNonGenericTask()
        {
            called = true;
            return Task.FromResult(1);
        }
        
        await CommandLineHostBuilder.Create(Array.Empty<string>())
            .ConfigureServices((context, collection) =>
            {
                collection.AddScoped<IRandomTextService, RandomTextService>();
            })
            .WithRootCommandHandler(RootCommandWithNonGenericTask)
            .Build()
            .RunAsync();

        called.Should().BeTrue();
        Environment.ExitCode.Should().Be(1);
    }
}