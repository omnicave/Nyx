using System;
using System.CommandLine.Rendering;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Nyx.Cli.Logging;

public class NyxConsoleFormatter : ConsoleFormatter
{
    private readonly IOptionsMonitor<NyxConsoleFormatterOptions> _nyxConsoleOptions;
    internal const string FormatterName = "Nyx";
    public NyxConsoleFormatter(IOptionsMonitor<NyxConsoleFormatterOptions> nyxConsoleOptions) : base(FormatterName)
    {
        _nyxConsoleOptions = nyxConsoleOptions;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter
        )
    {
        // var sb = new StringBuilder(80);
        
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "<Unable to format log message>";

        if (!string.IsNullOrEmpty(_nyxConsoleOptions.CurrentValue.TimestampFormat))
            AnsiConsole.Console.MarkupInterpolated($"[silver]{DateTimeOffset.UtcNow.ToString(_nyxConsoleOptions.CurrentValue.TimestampFormat)}[/] ");

        AnsiConsole.Console.MarkupInterpolated($"[blue]{logEntry.LogLevel}[/] ");

        if (_nyxConsoleOptions.CurrentValue.IncludeCategory)
            AnsiConsole.Console.MarkupInterpolated($"[grey]{logEntry.Category}[/] ");
        AnsiConsole.Console.MarkupInterpolated($"[white]{message}[/]");
        AnsiConsole.WriteLine();
        // AnsiConsole.Console.Markup(sb.ToString());
    }
}

public class NyxConsoleFormatterOptions : ConsoleFormatterOptions
{
    public bool IncludeCategory { get; set; }
}