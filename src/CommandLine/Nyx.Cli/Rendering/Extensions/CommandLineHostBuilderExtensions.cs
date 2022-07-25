using Microsoft.Extensions.DependencyInjection;
using Nyx.Cli;
using Nyx.Cli.Rendering;

// ReSharper disable once CheckNamespace
namespace Nyx.Cli;

public static partial class CommandLineHostBuilderExtensions
{
    public static ICommandLineHostBuilder AddOutputFormatGlobalFlag(this ICommandLineHostBuilder builder)
    {
        return builder
            .ConfigureServices(
                (context, collection) => collection.AddOutputFormattingSupport()
            )
            .AddGlobalOption<OutputFormatOption>();
    }
}