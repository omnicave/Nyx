using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
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
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOutputFormattingSupport(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ICliRendererWithFormat, JsonCliRenderer>();
            serviceCollection.AddScoped<ICliRendererWithFormat, TabularCliRenderer>();
            serviceCollection.AddScoped<ICliRendererWithFormat, RawCliRenderer>();
            
            serviceCollection.AddScoped(
                provider =>
                {
                    var cliRenderers = provider.GetServices<ICliRendererWithFormat>();
                    var invocationContext = provider.GetRequiredService<IInvocationContext>();

                    if (!invocationContext.TryGetSingleOptionValue<OutputFormat>("output", out var outputFormat))
                        outputFormat = OutputFormat.raw;

                    return (ICliRenderer)cliRenderers.First(x => x.Format == outputFormat);
                }
            );

            return serviceCollection;
        }
    }
}