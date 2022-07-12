using System.Collections.ObjectModel;

namespace Nyx.Utils;

public static class DictionaryExtensions
{
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dict) 
        where TKey : notnull
    {
        return new ReadOnlyDictionary<TKey, TValue>(dict);
    }
}