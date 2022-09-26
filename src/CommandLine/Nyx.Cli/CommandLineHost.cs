using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nyx.Cli;

public class CommandLineHost : IHost
{
    private readonly ParseResult _parseResult;
    private int _exitCode;
    private readonly Parser _parser;

    internal CommandLineHost(CommandLineBuilder commandLineBuilder, string[] args)
    {
        _parser = commandLineBuilder.Build();
        _parseResult = _parser.Parse(args);

        Services = new ServiceCollection().BuildServiceProvider();
    }

    public void Dispose() { }

    public async Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        _exitCode = await _parseResult.InvokeAsync();
        Environment.ExitCode = _exitCode;
    }

    public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public IServiceProvider Services { get; }

    public int ExitCode => _exitCode;
    public CommandLineConfiguration Configuration => _parser.Configuration;
}