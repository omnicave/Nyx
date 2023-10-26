using Newtonsoft.Json;
using Nyx.Orleans.Nats.Clustering;
using Orleans;
using Orleans.GrainReferences;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Serialization.TypeSystem;

namespace Nyx.Orleans.Serialization;

public static class NewtonsoftJsonSerializerSettingsBuilder
{
    public static JsonSerializerSettings GetDefaults()
    {
        var jsonSerializerSettings = new JsonSerializerSettings();
        ConfigureJsonSerializerSettingsWithDefaults(jsonSerializerSettings);
        return jsonSerializerSettings;
    }
    
    public static JsonSerializerSettings GetDefaultsWithOrleansSupport()
    {
        var settings = GetDefaults();
        settings.Converters.Add(new EventSequenceTokenConverter());

        return settings;
    }

    public static JsonSerializerSettings GetDefaultsWithOrleansSupport(
        TypeResolver typeResolver,
        GrainReferenceActivator grainReferenceActivator
        )
    {
        var settings = GetDefaultsWithOrleansSupport();
        var serializationBinder = new OrleansJsonSerializationBinder(typeResolver);
        settings.SerializationBinder = serializationBinder;
        settings.Converters.Add(new GrainReferenceJsonConverter(grainReferenceActivator));

        return settings;
    }

    public static void ConfigureJsonSerializerSettingsWithDefaults(JsonSerializerSettings jsonSerializerSettings)
    {
        jsonSerializerSettings.TypeNameHandling = TypeNameHandling.All;
        jsonSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        jsonSerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        jsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
        jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
        jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        jsonSerializerSettings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        jsonSerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        jsonSerializerSettings.Formatting = Formatting.None;

        jsonSerializerSettings.Converters.Add(new IPAddressConverter());
        jsonSerializerSettings.Converters.Add(new IPEndPointConverter());
        jsonSerializerSettings.Converters.Add(new GrainIdConverter());
        jsonSerializerSettings.Converters.Add(new NewtonsoftJsonSiloAddressConverter());
        jsonSerializerSettings.Converters.Add(new UniqueKeyConverter());

    }
}