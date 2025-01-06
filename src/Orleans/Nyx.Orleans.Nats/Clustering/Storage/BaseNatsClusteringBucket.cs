using System.Text;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Newtonsoft.Json;
using Nyx.Orleans.Nats.Clustering.Storage.Models;
using Nyx.Orleans.Serialization;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Nyx.Orleans.Nats.Clustering;

public class BaseNatsClusteringBucket : IAsyncDisposable
{
    protected readonly NatsClusteringOptions NatsClusteringOptions;
    protected readonly ClusterOptions OrleansClusterOptions;
    private readonly NatsConnection _connection;
    
    private readonly JsonSerializerSettings _jsonSerializerSettings;
    private readonly NatsJSContext _jsContext;
    private readonly NatsKVContext _kvContext;
    private readonly NewtonsoftNatsSerializer<ClusteringEntryStorage> _serializer;

    protected BaseNatsClusteringBucket(IOptions<NatsClusteringOptions> natsClusteringOptions, IOptions<ClusterOptions> clusterOptions)
    {
        NatsClusteringOptions = natsClusteringOptions.Value;
        OrleansClusterOptions = clusterOptions.Value;

        _connection = new NatsConnection(new NatsOpts()
        {
            Url = NatsClusteringOptions.NatsUrl
        });

        _jsContext = new NatsJSContext(_connection);
        _kvContext = new NatsKVContext(_jsContext);
        
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

        _serializer = new NewtonsoftNatsSerializer<ClusteringEntryStorage>(_jsonSerializerSettings);
    }
    private string GetBucketName() =>
        $"{NatsClusteringOptions.BucketName}-{OrleansClusterOptions.ClusterId}-{OrleansClusterOptions.ServiceId}";

    protected ValueTask<INatsKVStore> GetBucket() => _kvContext.GetStoreAsync(GetBucketName());
    
    protected string GetKey(SiloAddress siloAddress) => siloAddress.ToParsableString()
        .Replace(':', '-')
        .Replace('@', '/');

    protected SiloAddress ParseKey(string key) => SiloAddress.FromParsableString(
        key.Replace('-', ':')
            .Replace('/', '@')
    );
    
    //
    public virtual async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
    
    private async Task EnsureBucketExists()
    {
        var result = _kvContext.GetBucketNamesAsync();
        var bucketNames = result.ToBlockingEnumerable().ToList();
        if (!bucketNames.Contains(GetBucketName()))
        {
            await _kvContext.CreateStoreAsync(new NatsKVConfig(GetBucketName())
            {
                Storage = NatsKVStorageType.Memory,
                MaxAge = TimeSpan.FromMinutes(1),
                History = 5
            });
        }
    }
    protected async Task Init()
    {
        await EnsureBucketExists();
    }

    protected async Task RefreshTtlForSiloEntry(SiloAddress siloAddress)
    {
        var kv = await GetBucket();

        var key = GetKey(siloAddress);
        var kve = await kv.GetEntryAsync<ClusteringEntryStorage>(key);
        if (kve.Value == null)
            return;
        await kv.PutAsync(key, kve.Value);
    }
    
    protected async Task Upsert(MembershipEntry entry, TableVersion tableVersion, string? etag = null, ulong? natsRevision = null)
    {
        var kv = await GetBucket();
        var w = new ClusteringEntryStorage(entry, tableVersion);
        await kv.PutAsync(GetKey(entry.SiloAddress), w, _serializer);
    }

    protected async Task<ClusteringEntry> Get(SiloAddress siloAddress)
    {
        var kv = await GetBucket();
        var w = await kv.GetEntryAsync<ClusteringEntryStorage>(GetKey(siloAddress));
        return DeserializeClusteringEntryStorage(w);
    }

    private ClusteringEntry DeserializeClusteringEntryStorage(NatsKVEntry<ClusteringEntryStorage> kve)
    {
        if (kve.Value == null) 
            throw new InvalidOperationException();
        
        var storedEntry = kve.Value;
        return new ClusteringEntry(storedEntry.Entry, storedEntry.TableVersion, kve.Revision);
    }

    protected async Task<IEnumerable<ClusteringEntry>> GetAll()
    {
        var kv = await GetBucket();
        
        var result = new List<ClusteringEntry>();

        await foreach (var item in kv.GetKeysAsync())
        {
            var e = await kv.GetEntryAsync<ClusteringEntryStorage>(item, serializer: _serializer );
            result.Add(DeserializeClusteringEntryStorage(e));
        }
        
        return result;
    }
}