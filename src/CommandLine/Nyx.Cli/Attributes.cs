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
    public class CliSubCommandArgumentAttribute : Attribute
    {   
    }
    

    [AttributeUsage(AttributeTargets.Property)]
    public class CliOptionAttribute : Attribute
    {
        public string Name { get; }

        public string Alias { get; set; } = string.Empty;
        
        public bool HasAlias => !(ReferenceEquals(Alias, string.Empty) || string.IsNullOrWhiteSpace(Alias));

        public CliOptionAttribute(string name)
        {
            Name = name;
        }
    }
}