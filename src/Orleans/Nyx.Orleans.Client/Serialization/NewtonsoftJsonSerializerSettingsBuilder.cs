using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Nyx.Orleans.Serialization;

public static class NewtonsoftJsonSerializerSettingsBuilder
{
    public static JsonSerializerSettings GetDefaults()
    {
        var jsonSerializerSettings = new JsonSerializerSettings();
        ConfigureJsonSerializerSettingsWithDefaults(jsonSerializerSettings);
        return jsonSerializerSettings;
    }

    public static JsonSerializerSettings GetDefaultsWithOrleansSupport(
        ITypeResolver typeResolver,
        IGrainFactory grainFactory
        )
    {
        var settings = GetDefaults();
        var serializationBinder = new OrleansJsonSerializationBinder(typeResolver);
        settings.SerializationBinder = serializationBinder;
        settings.Converters.Add(new GrainReferenceConverter(grainFactory, serializationBinder));
        settings.Converters.Add(new EventSequenceTokenConverter());

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
        jsonSerializerSettings.Converters.Add(new SiloAddressConverter());
        jsonSerializerSettings.Converters.Add(new UniqueKeyConverter());

    }
}