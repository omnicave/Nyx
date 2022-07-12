using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Nyx.Orleans.Serialization;

public class NewtonsoftJsonSerializer : IExternalSerializer
    {
        public const string UseFullAssemblyNamesProperty = "UseFullAssemblyNames";
        public const string IndentJsonProperty = "IndentJSON";
        public const string TypeNameHandlingProperty = "TypeNameHandling";
        private readonly Lazy<JsonSerializerSettings> _settings;

        public NewtonsoftJsonSerializer(IServiceProvider services)
        {
            this._settings = new Lazy<JsonSerializerSettings>(() =>
            {
                var typeResolver = services.GetRequiredService<ITypeResolver>();
                var grainFactory = services.GetRequiredService<IGrainFactory>();
                return GetDefaultSerializerSettings(typeResolver, grainFactory);
            });
        }

        /// <summary>
        /// Returns the default serializer settings.
        /// </summary>
        /// <returns>The default serializer settings.</returns>
        public static JsonSerializerSettings GetDefaultSerializerSettings(ITypeResolver typeResolver, IGrainFactory grainFactory)
        {
            var serializationBinder = new OrleansJsonSerializationBinder(typeResolver);
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Formatting = Formatting.None,
                SerializationBinder = serializationBinder
            };

            settings.Converters.Add(new IPAddressConverter());
            settings.Converters.Add(new IPEndPointConverter());
            settings.Converters.Add(new GrainIdConverter());
            settings.Converters.Add(new SiloAddressConverter());
            settings.Converters.Add(new UniqueKeyConverter());
            settings.Converters.Add(new GrainReferenceConverter(grainFactory, serializationBinder));

            return settings;
        }

        /// <summary>
        /// Customises the given serializer settings using provider configuration.
        /// Can be used by any provider, allowing the users to use a standard set of configuration attributes.
        /// </summary>
        /// <param name="settings">The settings to update.</param>
        /// <param name="config">The provider config.</param>
        /// <returns>The updated <see cref="JsonSerializerSettings" />.</returns>
        public static JsonSerializerSettings UpdateSerializerSettings(JsonSerializerSettings settings, IProviderConfiguration config)
        {
            bool useFullAssemblyNames = config.GetBoolProperty(UseFullAssemblyNamesProperty, false);
            bool indentJson = config.GetBoolProperty(IndentJsonProperty, false);
            TypeNameHandling typeNameHandling = config.GetEnumProperty(TypeNameHandlingProperty, settings.TypeNameHandling);
            return UpdateSerializerSettings(settings, useFullAssemblyNames, indentJson, typeNameHandling);
        }

        public static JsonSerializerSettings UpdateSerializerSettings(JsonSerializerSettings settings, bool useFullAssemblyNames, bool indentJson, TypeNameHandling? typeNameHandling)
        {
            if (useFullAssemblyNames)
            {
                settings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full;
            }

            if (indentJson)
            {
                settings.Formatting = Formatting.Indented;
            }

            if (typeNameHandling.HasValue)
            {
                settings.TypeNameHandling = typeNameHandling.Value;
            }
           
            return settings;
        }

        /// <inheritdoc />
        public bool IsSupportedType(Type itemType)
        {
            return true;
        }

        /// <inheritdoc />
        public object DeepCopy(object source, ICopyContext context)
        {
            if (source == null)
            {
                return null;
            }

            var outputWriter = new BinaryTokenStreamWriter();
            var serializationContext = new SerializationContext(context.GetSerializationManager())
            {
                StreamWriter = outputWriter
            };
            
            Serialize(source, serializationContext, source.GetType());
            var deserializationContext = new DeserializationContext(context.GetSerializationManager())
            {
                StreamReader = new BinaryTokenStreamReader(outputWriter.ToBytes())
            };

            var retVal = Deserialize(source.GetType(), deserializationContext);
            outputWriter.ReleaseBuffers();
            return retVal;
        }

        /// <inheritdoc />
        public object Deserialize(Type expectedType, IDeserializationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var reader = context.StreamReader;
            var str = reader.ReadString();
            return JsonConvert.DeserializeObject(str, expectedType, this._settings.Value);
        }

        /// <summary>
        /// Serializes an object to a binary stream
        /// </summary>
        /// <param name="item">The object to serialize</param>
        /// <param name="context">The serialization context.</param>
        /// <param name="expectedType">The type the deserializer should expect</param>
        public void Serialize(object item, ISerializationContext context, Type expectedType)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var writer = context.StreamWriter;
            if (item == null)
            {
                writer.WriteNull();
                return;
            }

            var str = JsonConvert.SerializeObject(item, expectedType, this._settings.Value);
            writer.Write(str);
        }
    }