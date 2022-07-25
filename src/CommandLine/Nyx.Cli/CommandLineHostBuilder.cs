using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Nyx.Cli.Logging;
using Nyx.Hosting;
using Spectre.Console;

namespace Nyx.Cli;

public interface ICommandLineHostBuilder
{
    ICommandLineHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
    ICommandLineHostBuilder RegisterCommandsFromThisAssembly();
    ICommandLineHostBuilder AddYamlConfigurationFile(string path);

    ICommandLineHostBuilder AddGlobalOption<TOption>()
        where TOption : Option, new();

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name
    );

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias
    );

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        string description
    );

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        TValue defaultValue  
    );

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        TValue defaultValue  
    );

    ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        string description,
        TValue defaultValue
    );

    ICommandLineHostBuilder AddConfigurationOptions<T>(string sectionName) where T : class;
    
    Task<int> RunAsync();

    ICommandLineHostBuilder UseHostBuilderFactory(Func<IInvocationContext, IHostBuilder> hostBuilderFactory);
}

public class CommandLineHostBuilder : BaseHostBuilder, ICommandLineHostBuilder
{
    public static ICommandLineHostBuilder Create(string name, string[] args) => new CommandLineHostBuilder(name, args);
    public static ICommandLineHostBuilder Create(string[] args) => new CommandLineHostBuilder(Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Nyx.Cli", args);

    private readonly string _name;
    private readonly string[] _args;
    
    private readonly List<Type> _commandTypes = new();
    private readonly List<Action<CommandLineBuilder>> _cliBuilderHandlers = new();
        
    private Func<IInvocationContext, IHostBuilder> _hostBuilderFactory = DefaultHostBuilderFactory;
    private Func<IHost, CancellationToken, Task> _hostStartupProc =
        (host, cancellationToken) => host.StartAsync(cancellationToken);
    private Func<IHost, CancellationToken, Task> _hostShutdownProc =
        (host, cancellationToken) => host.StopAsync(cancellationToken);

    private static readonly Func<IInvocationContext, IHostBuilder> DefaultHostBuilderFactory = (_ =>
            new HostBuilder()
                .ConfigureHostConfiguration(
                    config =>
                    {
                        config.AddEnvironmentVariables(prefix: "DOTNET_");
                    }
                )
                .ConfigureAppConfiguration(
                    (context, builder) =>
                    {
                        builder.AddEnvironmentVariables();
                    }
                )
        );

    private CommandLineHostBuilder(string name, string[] args)
    {
        _name = name;
        _args = args;
        _cliBuilderHandlers.Add(builder =>
        {
            builder
                .UseVersionOption()
                .UseHelp()
                .UseTypoCorrections()
                .UseParseErrorReporting();

            builder
                .UseExceptionHandler()
                .CancelOnProcessTermination();

            builder.EnableDirectives()
                .UseParseDirective()
                .UseSuggestDirective()
                .UseEnvironmentVariableDirective();
        });
    }

    public ICommandLineHostBuilder UseHostBuilderFactory(Func<IInvocationContext, IHostBuilder> hostBuilderFactory)
    {
        _hostBuilderFactory = hostBuilderFactory;
        return this;
    }

    public new ICommandLineHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        base.ConfigureServices(configureDelegate);
        return this;
    }

    public ICommandLineHostBuilder RegisterCommandsFromThisAssembly()
    {
        var assembly = Assembly.GetCallingAssembly();

        var commandTypes = assembly.GetTypes()
            .Select(t => (
                type: t, 
                attr: t.GetCustomAttribute<CliCommandAttribute>(),
                isCommand: t.GetInterfaces().Contains(typeof(ICliCommand))
                )
            )
            .Where(t => t.isCommand)
            .ToList();
            
        _commandTypes.AddRange(commandTypes.Select(x=>x.type));
            
        return this;
    }

    public override IHost Build() => throw new NotImplementedException("Call RunAsync() directly.");

    public async Task<int> RunAsync()
    {
        ConfigureServices((context, services) =>
        {
            services.AddScoped<IInvocationContext, InvocationContextHelper>();
            foreach (var item in _commandTypes)
                services.AddScoped(item);
        });
        
        // build cli root command
        var rootCommand = new Command(_name);
        var cliBuilder = new CommandLineBuilder(rootCommand);
        _cliBuilderHandlers.ForEach( x => x(cliBuilder) );
        
        foreach (var commandBuilder in _commandTypes
                     .Select(item => 
                         typeof(MethodInfoBasedCommandBuilder<>)
                         .MakeGenericType(item)
                         )
                     .Select(commandBuilderType => (ICommandBuilder?)Activator.CreateInstance(commandBuilderType, new object[] { rootCommand }) ?? throw new InvalidOperationException()))
        {
            rootCommand.AddCommand(commandBuilder.Build());
        }

        cliBuilder.AddMiddleware(
            async (context, next) =>
            {
                var argsRemaining = context.ParseResult.UnparsedTokens.ToArray();

                var invocationContext = new InvocationContextHelper(context);
                var hostBuilder = _hostBuilderFactory.Invoke(invocationContext);
                
                hostBuilder.Properties[typeof(InvocationContext)] = context;

                hostBuilder.ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsoleFormatter<NyxConsoleFormatter, NyxConsoleFormatterOptions>();
                    logging.AddConsole(options =>
                    {
                        options.FormatterName = NyxConsoleFormatter.FormatterName;
                    });
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
                hostBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(context);
                    services.AddSingleton(context.BindingContext);
                    services.AddSingleton(context.Console);
                    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
                    services.AddTransient<IInvocationResult>(_ => context.InvocationResult ?? throw new InvalidOperationException("Cannot obtain InvocationResult"));
                    services.AddTransient(_ => context.ParseResult);
                    
                    services.AddOutputFormattingSupport();
                    
                    if (invocationContext.TryGetSingleOptionValue<LogLevel>(LogLevelOption.OptionName, out var customLevel))
                    {
                        switch (customLevel)
                        {
                            case LogLevel.Trace:
                            case LogLevel.Debug:
                                services.Configure<NyxConsoleFormatterOptions>(options =>
                                {
                                    options.IncludeCategory = true;
                                    options.TimestampFormat = "s";
                                });
                                break;
                            case LogLevel.Information:
                                break;
                            case LogLevel.Warning:
                                break;
                            case LogLevel.Error:
                                break;
                            case LogLevel.Critical:
                                break;
                            case LogLevel.None:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    
                    services.AddOptions();
                });
                hostBuilder.UseInvocationLifetime(context);
                ApplyHostBuilderOperations(hostBuilder);
           
                using var host = hostBuilder.Build();
            
                // ReSharper disable once AccessToDisposedClosure
                context.BindingContext.AddService(typeof(IHost), _ => host);
            
                await _hostStartupProc(host, CancellationToken.None);
                
                try
                {
                    await next(context);
                }
                catch (Exception e)
                {
                    var renderer = host.Services.GetRequiredService<ICliRenderer>();
                    renderer.RenderError(e);
                    context.ExitCode = -1;
                }

                await _hostShutdownProc(host, CancellationToken.None);
            }
        );

        var parser = cliBuilder.Build();
        var parseResult = parser.Parse(_args);
        return await parseResult.InvokeAsync();
    }

    public ICommandLineHostBuilder AddYamlConfigurationFile(string path)
    {
        this.ConfigureAppConfiguration(
            (context, builder) =>
            {
                if (path.StartsWith("~"))
                {
                    var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    path = Path.Combine(userProfilePath, path[1..].TrimStart('/'));
                }

                builder.AddYamlFile(path, true);
            }
        );

        return this;
    }

    public ICommandLineHostBuilder AddGlobalOption<TOption>() where TOption : Option, new()
    {
        _cliBuilderHandlers.Add(builder => builder.Command.AddGlobalOption(new TOption()));
        return this;
    }

    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name
    ) => AddGlobalOptionInternal<TValue>(name, null, null, null);
        
    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias
    ) => AddGlobalOptionInternal<TValue>(name, alias, null, null);
        
    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        string description
    ) => AddGlobalOptionInternal<TValue>(name, alias, description, null);
        
    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        TValue defaultValue  
    ) => AddGlobalOptionInternal<TValue>(name, null, null, ()=>defaultValue);
        
    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        TValue defaultValue  
    ) => AddGlobalOptionInternal<TValue>(name, alias, null, ()=>defaultValue);
        
    public ICommandLineHostBuilder AddGlobalOption<TValue>(
        string name,
        string alias,
        string description,
        TValue defaultValue
    ) => AddGlobalOptionInternal<TValue>(name, alias, description, ()=>defaultValue);

    private ICommandLineHostBuilder AddGlobalOptionInternal<TValue>(
        string name, 
        string? alias,
        string? description, 
        Func<TValue>? defaultValueProc
    )
    {
        var aliases = string.IsNullOrWhiteSpace(alias)
            ? new[] { $"--{name}" }
            : new[] { $"--{name}", $"-{alias}" };
            
        _cliBuilderHandlers.Add(
            builder => builder.Command.AddGlobalOption(
                defaultValueProc == null
                    ? new Option<TValue>(aliases, description ?? string.Empty)
                    : new Option<TValue>(aliases, defaultValueProc, description ?? string.Empty)
            )
        );
        return this;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ICommandLineHostBuilder AddConfigurationOptions<T>(string sectionName) where T : class
    {
        this.ConfigureServices(
            (context, services) =>
            {
                services.AddOptions<T>().Bind(context.Configuration.GetSection(sectionName));
            }
        );

        return this;
    }
}