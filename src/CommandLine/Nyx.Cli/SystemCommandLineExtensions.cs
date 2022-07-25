using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyx.Cli;
using Nyx.Cli.Rendering;

// ReSharper disable once CheckNamespace
namespace System.CommandLine.Builder
{
    public static class CommandLineBuilderExtensions
    {
        public static CommandLineBuilder SetupCommandLineHost(this CommandLineBuilder b, Action<HostBuilderContext, IServiceCollection>? configureServices = null)
        {
            b.UseHost(
                    HostBuilderFactory.CreateHostBuilder,
                    builder =>
                    {
                        builder.ConfigureServices(
                            (context, services) =>
                            {
                                configureServices?.Invoke(context, services);
                                services.AddOutputFormattingSupport();
                            }
                        );
                    }
                );

            b.AddOutputFormatSelectionFlag();

            return b;
        }

        public static CommandLineBuilder AddOutputFormatSelectionFlag(this CommandLineBuilder builder)
        {
            builder.Command.AddGlobalOption(new OutputFormatOption());
            return builder;
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
}