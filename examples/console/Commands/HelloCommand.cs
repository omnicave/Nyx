using Nyx.Cli;

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

    public WorldCommand(IInvocationContext invocationContext)
    {
        _invocationContext = invocationContext;
    }
    
    public Task Execute(
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