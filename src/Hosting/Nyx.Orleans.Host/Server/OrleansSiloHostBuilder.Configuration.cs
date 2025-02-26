using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nyx.Orleans.Serialization;
using Orleans.Serialization;

namespace Nyx.Orleans.Host;

public partial class OrleansSiloHostBuilder
{
    public OrleansSiloHostBuilder ConfigureApplicationBuilder(Action<IApplicationBuilder> appBuilderConfiguration)
    {
        WebApplicationConfiguration.Add(appBuilderConfiguration);
        return this;
    }
    
    public OrleansSiloHostBuilder ConfigureWebApplication(Action<WebApplication> webApplicationConfiguration)
    {
        WebApplicationConfiguration.Add(webApplicationConfiguration);
        return this;
    }

    public OrleansSiloHostBuilder ConfigureNewtonsoftJsonSerializer(Action<MvcNewtonsoftJsonOptions> d)
    {
        _configureNewtonsoftJsonSerializerForWebApi = d ?? throw new ArgumentNullException(nameof(d));
        return this;
    }

    public OrleansSiloHostBuilder ConfigureSystemTextJsonSerializer(Action<JsonOptions> d)
    {
        _configureSystemTextJsonSerializerForWebApi = d ?? throw new ArgumentNullException(nameof(d));
        return this;
    }
    
    private Action<MvcNewtonsoftJsonOptions> _configureNewtonsoftJsonSerializerForWebApi = options =>
    {
        options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
        options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
        options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
        options.SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
        options.SerializerSettings.Formatting = Formatting.Indented;

        options.SerializerSettings.Converters.Add(new IPAddressConverter());
        options.SerializerSettings.Converters.Add(new IPEndPointConverter());
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        
    };
    private Action<JsonOptions> _configureSystemTextJsonSerializerForWebApi = options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    };

    internal Action<HostBuilderContext, ISiloBuilder>? ClusteringConfiguration;
    internal readonly List<Action<HostBuilderContext, ISiloBuilder>> SiloBuilderExtraConfiguration = new();
    internal Action<HostBuilderContext, ISiloBuilder> PubStoreConfiguration = (context, builder) => { };
    internal readonly List<Action<WebApplication>> WebApplicationConfiguration = new();
}