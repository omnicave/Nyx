using Microsoft.Extensions.Logging;
using Nyx.Cli;
using Nyx.Cli.Rendering;
using Spectre.Console;

namespace Nyx.Examples.Console.Commands;

[CliCommand("hello")]
public class HelloCommand : ICliCommand
{
    private readonly IInvocationContext _invocationContext;

    public HelloCommand(IInvocationContext invocationContext)
    {
        _invocationContext = invocationContext;
    }
    
    public Task Execute()
    {
        System.Console.WriteLine("Hello World");

        if (_invocationContext.TryGetSingleOptionValue("globalone", out var globalOption, GlobalOption.Unset))
            System.Console.WriteLine($"Global Option is set {globalOption}");
        
        return Task.CompletedTask;
    }   
    
    [CliSubCommand("world")]
    public Task World(
        GlobalOption globalone
    )
    {
        System.Console.WriteLine("Hello World");

        if (_invocationContext.TryGetSingleOptionValue("globalone", out GlobalOption globalOption))
            System.Console.WriteLine($"Global Option (from context provider) is set {globalOption}");
        
        System.Console.WriteLine($"Global Option (from arg) is set {globalone}");
        
        return Task.CompletedTask;
    }
}

[CliCommand("world")]
public class WorldCommand : ICliCommand
{
    private readonly IInvocationContext _invocationContext;
    private readonly ICliRenderer _cliRenderer;
    private readonly ILogger<WorldCommand> _logger;

    public WorldCommand(IInvocationContext invocationContext, ICliRenderer cliRenderer, ILogger<WorldCommand> logger)
    {
        _invocationContext = invocationContext;
        _cliRenderer = cliRenderer;
        _logger = logger;
    }
    
    public Task Execute(
        string token
    )
    {
        var x = new[]
        {
            new
            {
                username = "abc"
            },
            new
            {
                username = "def"
            }
        }.AsEnumerable();
        _cliRenderer.Render(x);
        return Task.CompletedTask;
    }
}

[CliCommand("arr")]
public class ArrCommand : ICliCommand
{
    public Task Execute([CliOption] string[] values)
    {
        throw new InvalidOperationException("Error testing command");
    }
}

[CliCommand("fil")]
public class FileCommand : ICliCommand
{
    public Task Execute(FileInfo file)
    {
        System.Console.WriteLine(file.FullName);
        return Task.CompletedTask;

    }
}

[CliCommand("error")]
public class ErrorCommand : ICliCommand
{
    public Task Execute()
    {
        throw new InvalidOperationException("Error testing command");
    }
}

[CliCommand("text")]
public class TextCommand : ICliCommand
{
    private readonly IAnsiConsole _console;
    private readonly IInvocationContext _invocationContext;

    public TextCommand(IAnsiConsole console, IInvocationContext invocationContext)
    {
        _console = console;
        _invocationContext = invocationContext;
    }
    public async Task Execute()
    {
        _console.WriteLine();
        _console.WriteLine("bold", Style.Parse("bold"));
        _console.WriteLine("invert", Style.Parse("invert"));
        _console.MarkupLine("[blue]blue text[/]");

        await _console.Progress()
            .AutoRefresh(false) // Turn off auto refresh
            .AutoClear(false) // Do not remove the task list when done
            .HideCompleted(false) // Hide tasks as they are completed
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(), // Task description
                new ProgressBarColumn(), // Progress bar
                new PercentageColumn(), // Percentage
                new RemainingTimeColumn(), // Remaining time
                new SpinnerColumn(), // Spinner
            })
            .StartAsync(async ctx =>
            {
                // Define tasks
                var task1 = ctx.AddTask("[green]Reticulating splines[/]");
                var task2 = ctx.AddTask("[green]Folding space[/]");

                while (!ctx.IsFinished)
                {
                    await Task.Delay(100);

                    task1.Increment(1.5);
                    task2.Increment(0.5);
                    
                    ctx.Refresh();
                }
            });

        _invocationContext.SetExitCode(1);
        
    }
}