using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;

namespace Nyx.Cli;

class InvocationContextHelper : IInvocationContext
{
    private readonly InvocationContext _invocationContext;

    public InvocationContextHelper(InvocationContext invocationContext )
    {
        _invocationContext = invocationContext;
    }

    public TValue GetSingleOptionValue<TValue>(string optionName)
    {
        var optResults = _invocationContext.ParseResult.RootCommandResult.Children
            .Concat(_invocationContext.ParseResult.CommandResult.Children)
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
        var optResults = _invocationContext.ParseResult.RootCommandResult.Children
            .Concat(_invocationContext.ParseResult.CommandResult.Children)
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
        var optResults = _invocationContext.ParseResult.CommandResult.Children
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
}

public interface IInvocationContext
{
    TValue GetSingleOptionValue<TValue>(string optionName);
    bool TryGetSingleOptionValue<TValue>(string optionName, out TValue? value);
    bool TryGetSingleOptionValue<TValue>(string optionName, out TValue value, TValue defaultValueIfNotFound);
}