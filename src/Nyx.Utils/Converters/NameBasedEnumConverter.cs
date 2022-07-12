using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Nyx.Utils.Converters;

public class NameBasedEnumConverter<TSource, TTarget> : IConverter<TSource, TTarget>
    where TSource : struct, Enum
    where TTarget : struct, Enum 
{
    private static readonly IReadOnlyDictionary<string, TSource> SourceMap;
    private static readonly IReadOnlyDictionary<string, TTarget> TargetMap;
    private static readonly IReadOnlyDictionary<TSource, TTarget> StaticMappings;
    
    static NameBasedEnumConverter()
    {
        SourceMap = Enum.GetValues<TSource>().ToDictionary(v => Enum.GetName(v) ?? v.ToString()).AsReadOnly();
        TargetMap = Enum.GetValues<TTarget>().ToDictionary(v => Enum.GetName(v) ?? v.ToString()).AsReadOnly();

        var commonKeys = TargetMap.Keys.Intersect(SourceMap.Keys).ToList();
        StaticMappings = commonKeys.Select(s => (sourceValue: SourceMap[s], targetValue: TargetMap[s]))
            .ToDictionary(x => x.sourceValue, x => x.targetValue)
            .AsReadOnly();
    }

    public TTarget Convert(TSource source)
    {
        if (StaticMappings.TryGetValue(source, out var target))
            return target;

        throw new ConverterException($"Cannot convert {source} to {typeof(TTarget).Name} enum.");
    }
}