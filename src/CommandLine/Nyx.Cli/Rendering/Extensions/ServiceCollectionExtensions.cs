using System.Linq;
using Nyx.Cli;
using Nyx.Cli.Rendering;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

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