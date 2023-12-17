using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace Nyx.Cli;

public class CommandLineHost : IHost
{
    internal static IHost PrimaryInstance
    {
        get
        {
            if (_primaryInstance == null)
                throw new InvalidOperationException("Primary Host Instance not set.");
            return _primaryInstance;
        }
        set => _primaryInstance = value;
    }

    // private readonly ParseResult _parseResult;
    // private int _exitCode;
    // private readonly Parser _parser;
    private static IHost? _primaryInstance = null;

    internal CommandLineHost(Parser parser, IHost internalHost)
    {
        Configuration = parser.Configuration;
        PrimaryInstance = internalHost;
    }

    
    internal CommandLineConfiguration Configuration { get; }

    public void Dispose() { }

    public Task StartAsync(CancellationToken cancellationToken = new())
    {
        return PrimaryInstance.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = new())
    {
        return PrimaryInstance.StopAsync(cancellationToken);
    }

    public IServiceProvider Services => PrimaryInstance.Services;

    // public int ExitCode => _exitCode;
    // public CommandLineConfiguration Configuration => _parser.Configuration;
}