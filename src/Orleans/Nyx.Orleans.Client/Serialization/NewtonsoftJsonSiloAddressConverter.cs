using Newtonsoft.Json;

namespace Nyx.Orleans.Serialization;

public sealed class NewtonsoftJsonSiloAddressConverter : JsonConverter
{

    public override bool CanConvert(Type objectType) => objectType == typeof(SiloAddress);

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        => SiloAddress.FromParsableString(reader.Value?.ToString() ?? throw new ArgumentNullException(nameof(existingValue)));

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => writer.WriteValue((value as SiloAddress)?.ToParsableString() ?? throw new ArgumentOutOfRangeException(nameof(value)));

}