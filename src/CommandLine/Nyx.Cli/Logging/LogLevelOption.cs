using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Nyx.Cli.Logging;

public class LogLevelOption : Option<LogLevel>
{
    internal const string OptionName = "log-level";
    public LogLevelOption() : base(new[] {$"--{OptionName}"}, getDefaultValue: () => LogLevel.Information, "Set the level of information reported to the console.")
    {
        
    }
}