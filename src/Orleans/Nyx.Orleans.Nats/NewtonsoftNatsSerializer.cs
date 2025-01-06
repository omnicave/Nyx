using System.Buffers;
using CommunityToolkit.HighPerformance;
using NATS.Client.Core;
using Newtonsoft.Json;

namespace Nyx.Orleans.Nats;

internal class NewtonsoftNatsSerializer<T>(JsonSerializerSettings serializerSettings) : INatsSerializer<T>
{
    private readonly JsonSerializer _serializer = JsonSerializer.Create(serializerSettings);

    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        var s = bufferWriter.AsStream();
        using var sw = new StreamWriter(s);
        _serializer.Serialize(sw, value);
        sw.Flush();
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        var s = buffer.AsStream();
        using var sr = new StreamReader(s);
        using var reader = new JsonTextReader(sr);
        var result = _serializer.Deserialize<T>(reader);
        
        return result;
    }

    public INatsSerializer<T> CombineWith(INatsSerializer<T> next)
    {
        return next;
    }
}

internal class NonTypedNewtonsoftNatsSerializer(JsonSerializerSettings serializerSettings) : INatsSerializer<object>
{
    private readonly JsonSerializer _serializer = JsonSerializer.Create(serializerSettings);

    public void Serialize(IBufferWriter<byte> bufferWriter, object value)
    {
        var s = bufferWriter.AsStream();
        using var sw = new StreamWriter(s);
        _serializer.Serialize(sw, value);
        sw.Flush();
    }

    public object? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        var s = buffer.AsStream();
        using var sr = new StreamReader(s);
        using var reader = new JsonTextReader(sr);
        var result = _serializer.Deserialize(reader);
        
        return result;
    }

    public INatsSerializer<object> CombineWith(INatsSerializer<object> next)
    {
        return next;
    }
}