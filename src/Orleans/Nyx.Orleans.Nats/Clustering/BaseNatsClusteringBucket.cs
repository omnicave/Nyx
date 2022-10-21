using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using NATS.Client.KeyValue;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Nyx.Orleans.Nats.Clustering;

public class BaseNatsClusteringBucket : IKeyValueWatcher, IDisposable
{
    protected readonly NatsClusteringOptions NatsClusteringOptions;
    protected readonly ClusterOptions OrleansClusterOptions;
    private readonly IConnection _connection;
    private KeyValueWatchSubscription? _watcherHandle;

    protected readonly ConcurrentDictionary<SiloAddress, (MembershipEntry entry, ulong revision)> MembershipEntryMap = new();
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
        _jsonSerializerSettings.Converters.Add(new SiloAddressConverter());
        _jsonSerializerSettings.Converters.Add(new UniqueKeyConverter());
    }
    private string GetBucketName() =>
        $"{NatsClusteringOptions.BucketName}-{OrleansClusterOptions.ClusterId}-{OrleansClusterOptions.ServiceId}";

    //protected IConnection GetConnection() => _connection;

    protected IKeyValue GetBucket() =>
        _connection.CreateKeyValueContext(GetBucketName());
    
    public void Watch(KeyValueEntry kve)
    {
        var siloAddress = ParseKey(kve.Key);

        if (kve.Operation.Equals(KeyValueOperation.Put))
        {
            var entry = (Deserialize<MembershipEntry>(kve.Value), kve.Revision);
            MembershipEntryMap.AddOrUpdate(
                siloAddress,
                _ => entry,
                (_, _) => entry);
        }

        if (kve.Operation.Equals(KeyValueOperation.Delete) || kve.Operation.Equals(KeyValueOperation.Purge))
        {
            MembershipEntryMap.Remove(siloAddress, out _);
        }
    }

    public void EndOfData()
    {
        
    }

    protected string GetKey(SiloAddress siloAddress) => siloAddress.ToParsableString().Replace(':', '-').Replace('@', '/');

    protected SiloAddress ParseKey(string key) => SiloAddress.FromParsableString(key.Replace('-', ':').Replace('/', '@'));

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
        _watcherHandle?.Dispose();
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
    
    private (MembershipEntry entry, ulong revision) ReadMembershipEntry(IKeyValue kv, string key)
    {
        var kentry = kv.Get(key);
        return (Deserialize<MembershipEntry>(kentry.Value), kentry.Revision);
    }

    protected void Init()
    {
        EnsureBucketExists();

        var kv = GetBucket();
        foreach (var k in kv.Keys())
        {
            var pair = ReadMembershipEntry(kv, k);
            MembershipEntryMap.TryAdd(pair.entry.SiloAddress, pair);
        }
        
        _watcherHandle = kv.WatchAll(this);
    }
}