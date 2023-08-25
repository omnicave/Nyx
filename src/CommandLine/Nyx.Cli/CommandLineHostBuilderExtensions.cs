using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nyx.Cli;
using Nyx.Cli.CommandBuilders;
using Nyx.Cli.Logging;
using Nyx.Cli.Rendering;

// ReSharper disable once CheckNamespace
namespace Nyx.Cli;

public static partial class CommandLineHostBuilderExtensions
{
    public static ICommandLineHostBuilder WithRootCommandHandler(this ICommandLineHostBuilder builder, Delegate d)
    {
        ((CommandLineHostBuilder)builder).RootCommandBuilderFactory = name =>  new DelegateRootCommandBuilder(d, name);
        return builder;
    }
    
    public static ICommandLineHostBuilder WithRootCommandHandler<T>(this ICommandLineHostBuilder builder) where T : class
    {
        builder.ConfigureServices((context, collection) => collection.AddScoped<T>());
        ((CommandLineHostBuilder)builder).RootCommandBuilderFactory = name =>  new TypedRootCommandBuilder<T>(name);
        return builder;
    }

    public static ICommandLineHostBuilder WithRootCommandHandler<T>(this ICommandLineHostBuilder builder, Func<T> factory) where T : class
    {
        if (typeof(T) == typeof(Task)|| typeof(T).BaseType == typeof(Task))
        {
            return WithRootCommandHandler(builder, (Delegate)factory);
        }
        builder.ConfigureServices((context, collection) => collection.AddScoped<T>(provider => factory()));
        ((CommandLineHostBuilder)builder).RootCommandBuilderFactory = name =>  new TypedRootCommandBuilder<T>(name);
        return builder;
    }
    
    public static ICommandLineHostBuilder WithRootCommandHandler<T>(this ICommandLineHostBuilder builder, Func<IServiceProvider, T> factory) where T : class
    {
        builder.ConfigureServices((context, collection) => collection.AddScoped<T>(factory));
        ((CommandLineHostBuilder)builder).RootCommandBuilderFactory = name =>  new TypedRootCommandBuilder<T>(name);
        return builder;
    }

    public static T ConfigureLoggingDefaults<T>(this T hostBuilder)
        where T : IHostBuilder
    {
        hostBuilder.ConfigureLogging((hostingContext, logging) =>
        {
            var invocationContext =
                (InvocationContextHelper)hostingContext.Properties[CommandLineHostBuilder.InvocationContext];
            
            logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
            logging.AddConsoleFormatter<NyxConsoleFormatter, NyxConsoleFormatterOptions>();
            logging.AddConsole(options => { options.FormatterName = NyxConsoleFormatter.FormatterName; });
            logging.AddDebug();
            logging.AddEventSourceLogger();
            logging.Configure(options =>
            {
                options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                  | ActivityTrackingOptions.TraceId
                                                  | ActivityTrackingOptions.ParentId;
            });
            if (invocationContext.TryGetSingleOptionValue<LogLevel>(LogLevelOption.OptionName, out var customLevel))
            {
                logging.SetMinimumLevel(customLevel);
            }

            logging.AddFilter("Microsoft", LogLevel.Error);
            logging.AddFilter("System", LogLevel.Error);
        });

        return hostBuilder;
    } 
}