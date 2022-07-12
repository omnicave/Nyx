using System.CommandLine;

namespace Nyx.Cli.Rendering
{
    internal class OutputFormatOption : Option<OutputFormat>
    {
        public OutputFormatOption() : base(
            new[] { "--output", "-o" },
            () => OutputFormat.raw,
            "Format how to render the output of the commands."
        )
        {
            
        }
    }
}