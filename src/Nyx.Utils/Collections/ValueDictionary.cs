using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Nyx.Utils.Collections;

public sealed class ValueDictionary<TKey, TValue> : 
    IImmutableDictionary<TKey, TValue>,
    IEquatable<ValueDictionary<TKey, TValue>> 
    where TKey : notnull
{
    private readonly IImmutableDictionary<TKey, TValue> _items;
    private readonly int _hashCode;
    
    public static ValueDictionary<TKey, TValue> Empty { get; } = ImmutableDictionary<TKey, TValue>.Empty.AsValueDictionary();

    private ValueDictionary(IImmutableDictionary<TKey, TValue> items, int hashCode)
    {
        _items = items;
        _hashCode = hashCode;
    }
    
    // copy constructor
    public ValueDictionary(ValueDictionary<TKey, TValue> source)
        : this(source._items, source._hashCode) { }

    public ValueDictionary(IDictionary<TKey, TValue> dictionary)
        : this(dictionary.ToImmutableDictionary(), CalculateDictionaryHashCode(dictionary)) { }
    
    public ValueDictionary(Dictionary<TKey, TValue> dictionary)
        : this(dictionary.ToImmutableDictionary(), CalculateDictionaryHashCode(dictionary)) { }
    private static int CalculateDictionaryHashCode(IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
        unchecked
        {
            return source
                .Select(
                    pair => pair.Key.GetHashCode() ^ (pair.Value?.GetHashCode() ?? 0)
                )
                .Aggregate(0, (i, entryHash) => i ^ entryHash);
        }
    }
    
    internal static ValueDictionary<TKey, TValue> FromAnyEnumerable(IEnumerable<KeyValuePair<TKey, TValue>> e)
    {
        var items = e switch
        {
            IImmutableDictionary<TKey, TValue> immutableList => immutableList,
            IReadOnlyDictionary<TKey, TValue> roList => roList.ToImmutableDictionary(),
            _ => e.ToImmutableDictionary()
        };

        return new ValueDictionary<TKey, TValue>(items, CalculateDictionaryHashCode(items));
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _items.Count;

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            null => false,
            ValueDictionary<TKey, TValue> c => Equals(c),
            _ => false
        };
    }

    public override int GetHashCode() => _hashCode;

    public static bool operator ==(ValueDictionary<TKey, TValue>? left, ValueDictionary<TKey, TValue>? right) 
        => Equals(left, right);
    public static bool operator !=(ValueDictionary<TKey, TValue>? left, ValueDictionary<TKey, TValue>? right) 
        => !Equals(left, right);
    
    public static implicit operator ValueDictionary<TKey, TValue>(ImmutableDictionary<TKey, TValue> e)
        => FromAnyEnumerable(e);

    public static implicit operator ValueDictionary<TKey, TValue>(Dictionary<TKey, TValue> e)
        => FromAnyEnumerable(e);

    public static implicit operator ValueDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> e)
        => FromAnyEnumerable(e);
    public bool Equals(ValueDictionary<TKey, TValue>? other)
        => other != null && other._hashCode.Equals( _hashCode );
    
    public bool ContainsKey(TKey key) => _items.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_items.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }

        value = default(TValue)!;
        return false;
    }

    public TValue this[TKey key] => _items[key];

    public IEnumerable<TKey> Keys => _items.Keys;
    public IEnumerable<TValue> Values => _items.Values;
    public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value) => _items.Add(key, value).AsValueDictionary();
    public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        => _items.AddRange(pairs).AsValueDictionary();

    public IImmutableDictionary<TKey, TValue> Clear()
        => _items.Clear().AsValueDictionary();

    public bool Contains(KeyValuePair<TKey, TValue> pair)
        => _items.Contains(pair);

    public IImmutableDictionary<TKey, TValue> Remove(TKey key)
        => _items.Remove(key).AsValueDictionary();

    public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        => _items.RemoveRange(keys).AsValueDictionary();

    public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        => _items.SetItem(key, value).AsValueDictionary();

    public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        => _items.SetItems(items).AsValueDictionary();

    public bool TryGetKey(TKey equalKey, out TKey actualKey)
        => _items.TryGetKey(equalKey, out actualKey);
}