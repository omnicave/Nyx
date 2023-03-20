using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;


namespace Nyx.Orleans.Serialization;

public class EventSequenceTokenConverter : JsonConverter
{
    private static readonly Type[] SupportedTypes = new[]
    {
        typeof(StreamSequenceToken),
        typeof(EventSequenceToken),
        typeof(EventSequenceTokenV2)
    };

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        
        if (value is StreamSequenceToken common)
        {
            
            writer.WritePropertyName(nameof(common.EventIndex));
            writer.WriteValue(common.EventIndex);
            writer.WritePropertyName(nameof(common.SequenceNumber));
            writer.WriteValue(common.SequenceNumber);
        }

        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);

        if (objectType == typeof(EventSequenceToken))
            return new EventSequenceToken(
                jo[nameof(EventSequenceToken.SequenceNumber)]?.ToObject<long>() ??
                throw new InvalidOperationException(),
                jo[nameof(EventSequenceToken.EventIndex)]?.ToObject<int>() ?? 0
            );
        
        if (objectType == typeof(EventSequenceTokenV2) || objectType == typeof(StreamSequenceToken))
            return new EventSequenceTokenV2(
                jo[nameof(EventSequenceTokenV2.SequenceNumber)]?.ToObject<long>() ??
                throw new InvalidOperationException(),
                jo[nameof(EventSequenceTokenV2.EventIndex)]?.ToObject<int>() ?? 0
            );

        throw new InvalidOperationException("Unsuported type.");
    }

    public override bool CanConvert(Type objectType)
    {
        return SupportedTypes.Contains(objectType);
    }
}