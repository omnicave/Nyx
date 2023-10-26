using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using NATS.Client.KeyValue;
using Newtonsoft.Json;
using Nyx.Orleans.Nats.Clustering.Storage.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Nyx.Orleans.Nats.Clustering;

public class BaseNatsClusteringBucket : IDisposable
{
    protected readonly NatsClusteringOptions NatsClusteringOptions;
    protected readonly ClusterOptions OrleansClusterOptions;
    private readonly IConnection _connection;
    
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    protected BaseNatsClusteringBucket(IOptions<NatsClusteringOptions> natsClusteringOptions, IOptions<ClusterOptions> clusterOptions)
    {
        NatsClusteringOptions = natsClusteringOptions.Value;
        OrleansClusterOptions = clusterOptions.Value;
        
        var factory = new ConnectionFactory();
        _connection = factory.CreateConnection(NatsClusteringOptions.NatsUrl);
        
        _jsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.None
        };

        _jsonSerializerSettings.Converters.Add(new IPAddressConverter());
        _jsonSerializerSettings.Converters.Add(new IPEndPointConverter());
        _jsonSerializerSettings.Converters.Add(new GrainIdConverter());
        _jsonSerializerSettings.Converters.Add(new NewtonsoftJsonSiloAddressConverter());
        _jsonSerializerSettings.Converters.Add(new UniqueKeyConverter());
    }
    private string GetBucketName() =>
        $"{NatsClusteringOptions.BucketName}-{OrleansClusterOptions.ClusterId}-{OrleansClusterOptions.ServiceId}";

    protected IKeyValue GetBucket() => _connection.CreateKeyValueContext(GetBucketName());
    
    protected string GetKey(SiloAddress siloAddress) => siloAddress.ToParsableString()
        .Replace(':', '-')
        .Replace('@', '/');

    protected SiloAddress ParseKey(string key) => SiloAddress.FromParsableString(
        key.Replace('-', ':')
            .Replace('/', '@')
    );

    protected byte[] Serialize<T>(T o)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o, _jsonSerializerSettings));
    }

    protected T Deserialize<T>(byte[] buffer)
    {
        var o = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer), _jsonSerializerSettings) ?? throw new InvalidOperationException();
        return o;
    }
    
    public virtual void Dispose()
    {
        _connection.Dispose();
    }
    
    private void EnsureBucketExists()
    {
        var kvm = _connection.CreateKeyValueManagementContext();

        var kvc = KeyValueConfiguration.Builder()
            .WithName(GetBucketName())
            .WithMaxHistoryPerKey(5)
            .WithTtl(Duration.OfMinutes(1))
            .WithStorageType(StorageType.Memory)
            .Build();

        var kvs = kvm.Create(kvc);
    }
    protected void Init()
    {
        EnsureBucketExists();
    }

    protected void RefreshTtlForSiloEntry(SiloAddress siloAddress)
    {
        var kv = GetBucket();

        var key = GetKey(siloAddress);
        var kve = kv.Get(key);
        if (kve == null)
            return;
        
        kv.Put(key, kve.Value);
    }
    
    protected void Upsert(MembershipEntry entry, TableVersion tableVersion, string? etag = null, ulong? natsRevision = null)
    {
        var kv = GetBucket();
        var w = new ClusteringEntryStorage(entry, tableVersion);
        kv.Put(GetKey(entry.SiloAddress), Serialize(w));
    }

    protected ClusteringEntry Get(SiloAddress siloAddress)
    {
        var kv = GetBucket();
        var w = kv.Get(GetKey(siloAddress));
        return DeserializeClusteringEntryStorage(w);
    }

    private ClusteringEntry DeserializeClusteringEntryStorage(KeyValueEntry kve)
    {
        var storedEntry = Deserialize<ClusteringEntryStorage>(kve.Value);
        return new ClusteringEntry(storedEntry.Entry, storedEntry.TableVersion, kve.Revision);
    }

    protected IEnumerable<ClusteringEntry> GetAll()
    {
        var kv = GetBucket();
        var result = kv.Keys().Select(ReadMembershipEntry).ToList().AsReadOnly();
        return result;

        ClusteringEntry ReadMembershipEntry(string key)
        {
            var kentry = kv.Get(key);
            return DeserializeClusteringEntryStorage(kentry);
        }
    }
}