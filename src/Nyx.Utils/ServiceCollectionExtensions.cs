using System.Reflection;
using Nyx.Utils;
using Nyx.Utils.Converters;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEnumConverter<TSource, TTarget>(this IServiceCollection collection) 
        where TTarget : struct, Enum where TSource : struct, Enum
    {
        return collection.AddTransient<IConverter<TSource, TTarget>, NameBasedEnumConverter<TSource, TTarget>>();
    }

    public static IServiceCollection AddConverter<TConverter>(this IServiceCollection collection) 
        where TConverter : class
    {
        var supportedConverterContracts = new[]
        {
            typeof(IConverter<,>),
            typeof(IAsyncConverter<,>)
        };
        
        var interfaces = typeof(TConverter).GetInterfaces();
        var converterInterfaces = interfaces
            .Where(i => i.IsConstructedGenericType && supportedConverterContracts.Contains(i.GetGenericTypeDefinition()) )
            .ToArray();

        if (!converterInterfaces.Any())
            throw new InvalidOperationException($"Converter {typeof(TConverter)} does not implement any IConverters.");
        
        collection.AddTransient<TConverter>();
        foreach (var converterInterface in converterInterfaces)
            collection.AddTransient(converterInterface, typeof(TConverter));

        return collection;
    }
}