using Newtonsoft.Json;
using Nyx.Orleans.Nats.Clustering;
using Nyx.Orleans.Serialization;
using Orleans.Serialization;

namespace Nyx.Orleans.Host.Internal;

public static class OrleansSerializationConfiguration
{
    public static IServiceCollection AddOrleansSerializationDefaults(this IServiceCollection s)
    {
        var jsonSettings = NewtonsoftJsonSerializerSettingsBuilder.GetDefaultsWithOrleansSupport();
        s.AddSerializer(
                builder => builder.AddNewtonsoftJsonSerializer(type => type?.FullName?.StartsWith("Nyx") ?? false,
                    jsonSettings)
            )
            .AddSerializer(
                b => b.AddNewtonsoftJsonSerializer(type => type.IsExceptionType(), jsonSettings)
            );

        return s;
    }
}