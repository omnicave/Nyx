using System;
using System.Reflection;

namespace Nyx.Cli;

/// <summary>
///     Denotes that this class declares a command for the CLI.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CliCommandAttribute : Attribute
{
    /// <summary>
    ///     Name of the command.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    ///     Short name for the command.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    public bool HasAlias => !(ReferenceEquals(Alias, string.Empty) || string.IsNullOrWhiteSpace(Alias));

    public CliCommandAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
///     The method is a sub-command, and it's name can be used to invoke from the CLI.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CliSubCommandAttribute : CliCommandAttribute
{
    internal static readonly CliSubCommandAttribute Default = new("EMPTY");
        
    public CliSubCommandAttribute(string name) : base(name)
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
[Obsolete("Replace with CliArgumentAttribute.")]
public class CliSubCommandArgumentAttribute : Attribute
{   
}

/// <summary>
///     Marks the parameter of a method as an argument, non-optional input, required for the subcommand / command. 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class CliArgumentAttribute : Attribute
{   
}
    

/// <summary>
///     Marks the parameter of a method as an optional argument for the subcommand / command invoked via CLI. 
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class CliOptionAttribute : Attribute
{
    // public string Name { get; }

    public string Alias { get; set; } = string.Empty;
        
    public bool HasAlias => !(ReferenceEquals(Alias, string.Empty) || string.IsNullOrWhiteSpace(Alias));

    public CliOptionAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class CliGlobalOptionAttribute : Attribute
{
    public string Name { get; set; }
    public TypeInfo Type { get; set; }
    
    public string? Description { get; set; }

    public string Alias { get; set; } = string.Empty;
        
    public bool HasAlias => !(ReferenceEquals(Alias, string.Empty) || string.IsNullOrWhiteSpace(Alias));

    public CliGlobalOptionAttribute(string name, TypeInfo type)
    {
        Name = name;
        Type = type;
    }
}

/// <summary>
///     Used on Commands and Sub commands to provide a host builder factory.
/// </summary>
public class CliHostBuilderFactoryAttribute : Attribute
{
    internal readonly ICliHostBuilderFactory Instance;

    public CliHostBuilderFactoryAttribute(Type hostBuilderFactoryType, params object[] args)
    {
        Instance = (ICliHostBuilderFactory?)Activator.CreateInstance(hostBuilderFactoryType, args) ?? throw new InvalidOperationException();
    }
}