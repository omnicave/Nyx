using System;

namespace Nyx.Cli
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CliCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Alias { get; set; } = string.Empty;

        public bool HasAlias => !(ReferenceEquals(Alias, string.Empty) || string.IsNullOrWhiteSpace(Alias));

        public CliCommandAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CliSubCommandAttribute : CliCommandAttribute
    {
        internal static CliSubCommandAttribute Default = new("EMPTY");
        
        public CliSubCommandAttribute(string name) : base(name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    [Obsolete]
    public class CliSubCommandArgumentAttribute : Attribute
    {   
    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CliArgumentAttribute : Attribute
    {   
    }
    

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
}