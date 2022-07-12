using System;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nyx.Data
{
    public static class PropertyBuilderExtensions
    {
        public static PropertyBuilder<T> StoreAsStringEnum<T>(this PropertyBuilder<T> builder)
            where T: Enum
        {
            return builder.HasConversion(
                    type => type.ToString(),
                    s => (T) Enum.Parse(typeof(T), s)
                );
        } 
        
    }
}