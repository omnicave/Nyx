using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nyx.Cli.CommandBuilders;
using Nyx.Cli.Internal;
using Nyx.Cli.Logging;
using Nyx.Cli.Rendering;
using Nyx.Hosting;
using Spectre.Console;

namespace Nyx.Cli;

public interface ICommandLineHostBuilder : IHostBuilder
{
    new ICommandLineHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate);
    
    ICommandLineHostBuilder RegisterCommandsFromThisAssembly();
    ICommandLineHostBuilder RegisterCommand<T>()
        where T : class;

    ICommandLineHostBuilder RegisterCommand(Type t);
    
    ICommandLineHostBuilder AddYamlConfigurationFile(string path);

    [Obsolete("Depends on underlying framework.")]
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
    
    // Task<int> RunAsync();

    ICommandLineHostBuilder UseHostBuilderFactory(Func<IInvocationContext, IHostBuilder> hostBuilderFactory);
}

internal class CliHostBuilderContext
{
    public List<Option> GlobalOptions { get; } = new ();
}

public class CommandLineHostBuilder : BaseHostBuilder, ICommandLineHostBuilder
{
    internal const string InvocationContext = "InvocationContext";
    
    public static ICommandLineHostBuilder Create(string name, string[] args) => new CommandLineHostBuilder(name, args);
    public static ICommandLineHostBuilder Create(string[] args) => new CommandLineHostBuilder(Assembly.GetEntryAssembly()?.GetName()?.Name ?? "Nyx.Cli", args);

    private readonly string _name;
    private readonly string[] _args;
    
    private readonly List<(Type commandType, Func<CliHostBuilderContext, Command, ICommandBuilder> commandBuilderHandler)> _commands = new();
    private readonly List<Action<CliHostBuilderContext, CommandLineBuilder>> _cliBuilderHandlers = new();

    internal ICliHostBuilderFactory HostBuilderFactory = new DefaultCliHostBuilderFactory();
    private Func<IHost, CancellationToken, Task> _hostStartupProc =
        (host, cancellationToken) => host.StartAsync(cancellationToken);
    private Func<IHost, CancellationToken, Task> _hostShutdownProc =
        (host, cancellationToken) => host.StopAsync(cancellationToken);

    public static readonly Func<IInvocationContext, IHostBuilder> DefaultHostBuilderFactory = (_ =>
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
        _cliBuilderHandlers.Add( (_, builder) =>
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
        HostBuilderFactory = new ActionBasedCliHostBuilderFactory(hostBuilderFactory);
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
                attr: t.GetCustomAttribute<CliCommandAttribute>()
                )
            )
            .Where(t => t.attr != null)
            .ToList();
            
        commandTypes.Select(x=>x.type).ToList().ForEach( t=> RegisterCommand(t));
            
        return this;
    }

    public ICommandLineHostBuilder RegisterCommand<T>()
        where T : class =>
        RegisterCommandInternal<T>();

    public ICommandLineHostBuilder RegisterCommand(Type t)
    {
        var mi = GetType()
            .GetMethod(
                nameof(RegisterCommandInternal), 
                BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance
                )?
            .MakeGenericMethod(t);

        if (mi == null)
            throw new InvalidOperationException("Could not register typed command due - invalid method info");

        var result = mi.Invoke(this, Array.Empty<object?>());

        if (result is ICommandLineHostBuilder r)
            return r;

        throw new InvalidOperationException("Invalid result from RegisterCommandInternal()");
    }

    private ICommandLineHostBuilder RegisterCommandInternal<T>()
        where T : class
    {
        _commands.Add(( typeof(T), (ctx, rootCommand) => new TypedChildCommandBuilder<T>(rootCommand, HostBuilderFactory) ) );
        return this;
    }

    private IHostBuilder BuildInternalHostBuilder(InvocationContextHelper invocationContext)
    {
        Func<IInvocationContext, IHostBuilder>? resolvedHostBuilderFactory = null;

        var isHelpRequested = invocationContext.ParseResult.CommandResult.Children.Any(x =>
            x.Symbol.Name.Equals("help", StringComparison.OrdinalIgnoreCase));

        if (isHelpRequested)
        {
            return DefaultHostBuilderFactory(invocationContext);
        }

        if (invocationContext.ParseResult.CommandResult.Command is INyxSystemConsoleCommand c)
            resolvedHostBuilderFactory = c.HostBuilderFactory;

        if (resolvedHostBuilderFactory == null)
            resolvedHostBuilderFactory = (context => HostBuilderFactory.CreateHostBuilder(context));
        
        var hostBuilder = resolvedHostBuilderFactory(invocationContext);

        this.Properties[InvocationContext] = invocationContext;
        hostBuilder.Properties[InvocationContext] = invocationContext;
        hostBuilder.Properties["CliHostBuilder"] = true;
        
        ApplyHostBuilderOperations(hostBuilder);
        
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton(invocationContext.ParseResult);
            services.AddSingleton<IInvocationContext>(invocationContext);
            services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
            
            services.AddOutputFormattingSupport();

            if (invocationContext.TryGetSingleOptionValue<LogLevel>(LogLevelOption.OptionName,
                    out var customLevel))
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
        
        ApplyHostBuilderAppOperations(hostBuilder);

        return hostBuilder;
    }

    public override IHost Build()
    {
        var cliBuilder = BuildCommandLineBuilder();
        var parser = cliBuilder.Build();

        var parseResult = parser.Parse(_args);

        var invocationContext = new InvocationContextHelper(parseResult);

        var internalHostBuilder = BuildInternalHostBuilder(invocationContext);
        internalHostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton(_ => parseResult);
            
            services.AddSingleton<IInvocationContext>(invocationContext);
            foreach (var item in _commands)
                services.AddSingleton(item.commandType);
        });
        
        var host = internalHostBuilder.Build();

        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted
            .Register(o =>
            {
                var (p, l) = ((ParseResult p, IHostApplicationLifetime l ))(o ?? throw new InvalidOperationException());
                Environment.ExitCode = p.Invoke();
                l.StopApplication();
            },
            (parseResult, lifetime)
        );
        
        return new CommandLineHost(parser, host);
    }

    private CommandLineBuilder BuildCommandLineBuilder()
    {
        // build cli root command
        var rootCommand = (RootCommandBuilderFactory != null)
            ? RootCommandBuilderFactory(_name).Build()
            : new Command(_name);

        var cliBuilder = new CommandLineBuilder(rootCommand);
        var cliHostBuilderContext = new CliHostBuilderContext();

        // apply all operations on CommandLineBuilder
        _cliBuilderHandlers.ForEach(x => x(cliHostBuilderContext, cliBuilder));

        // sub commands are added after the CommandLineBuilder actions are executed.  We config the global options in
        // the handlers, and they need to be available to the CommandBuilders
        foreach (var pair in _commands)
        {
            var commandBuilder = pair.commandBuilderHandler(cliHostBuilderContext, rootCommand);
            rootCommand.AddCommand(commandBuilder.Build(cliHostBuilderContext));
        }

        cliBuilder.AddMiddleware(
            async (context, next) =>
            {
                var argsRemaining = context.ParseResult.UnparsedTokens.ToArray();

                // ReSharper disable once AccessToDisposedClosure
                context.BindingContext.AddService(typeof(IHost), _ => CommandLineHost.PrimaryInstance);

                var c = (InvocationContextHelper)CommandLineHost.PrimaryInstance.Services
                    .GetRequiredService<IInvocationContext>();
                
                c.SetInvocationContext(context);
                //await _hostStartupProc(host, CancellationToken.None);

                try
                {
                    await next(context);
                }
                catch (Exception e)
                {
                    var renderer = CommandLineHost.PrimaryInstance.Services.GetRequiredService<ICliRenderer>();
                    renderer.RenderError(e);
                    context.ExitCode = -1;
                }

                //await _hostShutdownProc(host, CancellationToken.None);
            }
        );
        return cliBuilder;
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

    [Obsolete("Depends on underlying framework.")]
    public ICommandLineHostBuilder AddGlobalOption<TOption>() where TOption : Option, new()
    {
        _cliBuilderHandlers.Add((_, builder) => builder.Command.AddGlobalOption(new TOption()));
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
            (ctx, builder) =>
            {
                var opt = defaultValueProc == null
                    ? new Option<TValue>(aliases, description ?? string.Empty)
                    : new Option<TValue>(aliases, defaultValueProc, description ?? string.Empty);

                // register the global option into the context
                ctx.GlobalOptions.Add(opt);
                builder.Command.AddGlobalOption(opt);
            });
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

    internal Func<string, IRootCommandBuilder>? RootCommandBuilderFactory { get; set; } 
}