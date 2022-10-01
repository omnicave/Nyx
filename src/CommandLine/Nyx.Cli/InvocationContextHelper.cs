using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;

namespace Nyx.Cli;

class InvocationContextHelper : IInvocationContext
{
    private readonly ParseResult _parseResult;
    private InvocationContext? _invocationContext = null;

    public InvocationContextHelper(ParseResult parseResult)
    {
        _parseResult = parseResult;
    }
    
    public void SetInvocationContext(InvocationContext invocationContext)
    {
        if (_invocationContext != null)
            throw new InvalidOperationException("InvocationContext cannot be set twice.");

        _invocationContext = invocationContext;
    } 
    
    public TValue GetSingleOptionValue<TValue>(string optionName)
    {
        // if (_invocationContext == null)
        //     throw new InvalidOperationException("InvocationContext not set");
        
        var optResults = _parseResult.RootCommandResult.Children
            .Concat(_parseResult.CommandResult.Children)
            .OfType<OptionResult>()
            .Where(c => c.Symbol is Option<TValue> && c.Option.Name.Equals(optionName))
            .ToArray();

        if (optResults.Length == 0)
            throw new ArgumentOutOfRangeException($"Could not find an option with type {typeof(TValue)}");

        var opt = optResults.First();
        return opt.GetValueOrDefault<TValue>() ?? throw new ArgumentOutOfRangeException($"Option '{opt.Option.Name}' does not have a value.");
    }
    
    public bool TryGetSingleOptionValue<TValue>(string optionName, out TValue? value)
    {
        // if (_invocationContext == null)
        //     throw new InvalidOperationException("InvocationContext not set");
        
        var optResults = _parseResult.RootCommandResult.Children
            .Concat(_parseResult.CommandResult.Children)
            .OfType<OptionResult>()
            .Where(c => c.Symbol is Option<TValue> && c.Option.Name.Equals(optionName))
            .ToArray();
        
        if (optResults.Length == 0)
        {
            value = default(TValue);
            return false;
        }

        var opt = optResults.First();
        var v = opt.GetValueOrDefault<TValue>();

        value = v;
        return true;
    }

    public bool TryGetSingleOptionValue<TValue>(string optionName, out TValue value, TValue defaultValueIfNotFound)
    {
        // if (_invocationContext == null)
        //     throw new InvalidOperationException("InvocationContext not set");
        
        var optResults = _parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .Where(c => c.Symbol is Option<TValue> && c.Option.Name.Equals(optionName))
            .ToArray();

        if (optResults.Length == 0)
        {
            value = defaultValueIfNotFound;
            return false;
        }
        
        var opt = optResults.First();
        var v = opt.GetValueOrDefault<TValue>();
        if (v == null)
        {
            value = defaultValueIfNotFound;
            return false;
        }

        value = v;
        return true;
    }

    public void SetExitCode(int exitCode)
    {
        if (_invocationContext == null)
            throw new InvalidOperationException("InvocationContext not set");
        
        _invocationContext.ExitCode = exitCode;
    }
}

public interface IInvocationContext
{
    TValue GetSingleOptionValue<TValue>(string optionName);
    bool TryGetSingleOptionValue<TValue>(string optionName, out TValue? value);
    bool TryGetSingleOptionValue<TValue>(string optionName, out TValue value, TValue defaultValueIfNotFound);

    void SetExitCode(int exitCode);
}