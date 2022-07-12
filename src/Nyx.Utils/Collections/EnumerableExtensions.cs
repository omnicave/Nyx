using Nyx.Utils.Collections;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic;

public static class EnumerableExtensions
{
    public static ValueCollection<T> AsValueCollection<T>(this IEnumerable<T> e) 
        => ValueCollection<T>.FromAnyEnumerable(e);
    
    public static ValueDictionary<TKey, TValue> AsValueDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> e) 
        where TKey : notnull 
        => ValueDictionary<TKey, TValue>.FromAnyEnumerable(e);
}