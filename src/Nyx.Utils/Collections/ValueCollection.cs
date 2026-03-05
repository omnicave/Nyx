using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nyx.Utils.Collections;

using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

[JsonConverter(typeof(ValueCollectionJsonConverterFactory))]
public class ValueCollection<T> : IImmutableList<T>, IEquatable<ValueCollection<T>>
{
    private IImmutableList<T> _items;
    private int _hashCode;

    public static ValueCollection<T> Empty { get; } = Enumerable.Empty<T>().AsValueCollection();

    public ValueCollection(ValueCollection<T> source) : this(source._items, source._hashCode) { }
    
    // required for json serialization/deserialization
    public ValueCollection(IEnumerable<T> source) : this(FromAnyEnumerable(source)) { }
    public ValueCollection(IList<T> source) : this(FromAnyEnumerable(source)) { }
    
    [JsonConstructor]
    public ValueCollection(ICollection<T> source) : this(FromAnyEnumerable(source)) { }
    
    public ValueCollection(IImmutableList<T> source) : this(FromAnyEnumerable(source)) { }
    
    public ValueCollection(T[] source) : this(FromAnyEnumerable(source)) { }
    public ValueCollection() : this(FromAnyEnumerable([])) { }
    
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
    
    // ReSharper disable once NonReadonlyMemberInGetHashCode
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

public class ValueCollectionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return (typeToConvert.GetGenericTypeDefinition() == typeof(ValueCollection<>));
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var args = typeToConvert.GetGenericArguments();
        var collectionValueType = args[0];

        var converterType = typeof(ValueCollectionConverter<>).MakeGenericType(collectionValueType);

        return (JsonConverter)(Activator.CreateInstance(converterType) ?? throw new InvalidOperationException());
    }
}

public class ValueCollectionConverter<TValue> : JsonConverter<ValueCollection<TValue>>
{
    public override ValueCollection<TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var innerConverter = (JsonConverter<TValue>)options.GetConverter(typeof(TValue));
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException(); // Unexpected token type.  JsonTokenType.Null is handled by the framework, unless we set HandleNull => true (which we didn't).
        var list = new List<TValue>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            var item = innerConverter.Read(ref reader, typeof(TValue), options);
            // TODO: optionally add checks to make sure innerConverter correctly advanced the reader to the end of the current token.
            list.Add(item!);
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, ValueCollection<TValue> value, JsonSerializerOptions options)
    {
        var innerConverter = (JsonConverter<TValue>)options.GetConverter(typeof(TValue));

        
        writer.WriteStartArray();
        foreach (var item in value)
            innerConverter.Write(writer, item, options);
        
        writer.WriteEndArray();
    }
}