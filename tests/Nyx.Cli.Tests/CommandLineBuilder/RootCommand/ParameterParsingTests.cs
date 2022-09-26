using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
            .RunAsync();

        mock.Verify();
    }
}