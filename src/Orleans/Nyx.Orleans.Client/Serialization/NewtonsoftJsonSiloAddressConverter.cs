using Newtonsoft.Json;
using Orleans.Runtime;

namespace Nyx.Orleans.Nats.Clustering;

public sealed class NewtonsoftJsonSiloAddressConverter : JsonConverter
{

    override public bool CanConvert(Type objectType) => objectType == typeof(SiloAddress);

    override public object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        => SiloAddress.FromParsableString(reader.Value.ToString()!);

    override public void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => writer.WriteValue(((SiloAddress)value).ToParsableString());

}