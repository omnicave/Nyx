namespace Nyx.Utils.Collections;

using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

public sealed class ValueCollection<T> : IImmutableList<T>, IEquatable<ValueCollection<T>>
{
    private readonly IImmutableList<T> _items;
    private readonly int _hashCode;

    public static ValueCollection<T> Empty { get; } = Enumerable.Empty<T>().AsValueCollection();

    public ValueCollection(ValueCollection<T> source) : this(source._items, source._hashCode) { }
    
    // required for json serialization/deserialization
    public ValueCollection(IEnumerable<T> source) : this(FromAnyEnumerable(source)) { }
    
    // equality operators
    public static bool operator ==(ValueCollection<T>? left, ValueCollection<T>? right) 
        => Equals(left, right);
    public static bool operator !=(ValueCollection<T>? left, ValueCollection<T>? right) 
        => !Equals(left, right);
    
    // implicit converter operators to make assignments easy
    public static implicit operator ValueCollection<T>(T[] e)
        => FromAnyEnumerable(e);

    public static implicit operator ValueCollection<T>(List<T> e)
        => FromAnyEnumerable(e);

    public static implicit operator ValueCollection<T>(ReadOnlyCollection<T> e)
        => FromAnyEnumerable(e);

    private ValueCollection(IImmutableList<T> items, int hashCode)
    {
        _items = items;
        _hashCode = hashCode;
    }
    private static int CalculateHashOnEnumerable(IEnumerable<T> e)
    {
        unchecked
        {
            return e.Aggregate(19, (h, i) => h ^ (19 + (i?.GetHashCode() ?? 0)));    
        }
    }

    internal static ValueCollection<T> FromAnyEnumerable(IEnumerable<T> e)
    {
        var items = e switch
        {
            IImmutableList<T> immutableList => immutableList,
            IReadOnlyList<T> roList => roList.ToImmutableList(),
            _ => e.ToImmutableList()
        };

        return new ValueCollection<T>(items, CalculateHashOnEnumerable(items));
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public int Count => _items.Count;

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            null => false,
            ValueCollection<T> c => Equals(c),
            _ => false
        };
    }
    
    public override int GetHashCode() => _hashCode;
    public bool Equals(ValueCollection<T>? other) => other != null && other._hashCode.Equals(_hashCode);

    public T this[int index] => _items[index];
    
    public IImmutableList<T> Add(T value) => _items.Add(value).AsValueCollection();
    public IImmutableList<T> AddRange(IEnumerable<T> items) => _items.AddRange(items).AsValueCollection();
    public IImmutableList<T> Clear() => _items.Clear().AsValueCollection();
    public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
        => _items.IndexOf(item, index, count, equalityComparer);
    public IImmutableList<T> Insert(int index, T element) => _items.Insert(index, element).AsValueCollection();

    public IImmutableList<T> InsertRange(int index, IEnumerable<T> items) => _items.InsertRange(index, items).AsValueCollection();

    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
        => _items.LastIndexOf(item, index, count, equalityComparer);

    public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
        => _items.Remove(value, equalityComparer).AsValueCollection();

    public IImmutableList<T> RemoveAll(Predicate<T> match)
        => _items.RemoveAll(match).AsValueCollection();

    public IImmutableList<T> RemoveAt(int index)
        => _items.RemoveAt(index).AsValueCollection();

    public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        => _items.RemoveRange(items, equalityComparer).AsValueCollection();

    public IImmutableList<T> RemoveRange(int index, int count)
        => _items.RemoveRange(index, count).AsValueCollection();

    public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        => _items.Replace(oldValue, newValue, equalityComparer).AsValueCollection();

    public IImmutableList<T> SetItem(int index, T value)
        => _items.SetItem(index, value).AsValueCollection();
}