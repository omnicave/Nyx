using Nyx.Orleans.Serialization;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Streams;

namespace Nyx.Orleans.Host;

internal class ClusterClientFactory : IClusterClient
{
    private readonly ILoggerProvider _loggerProvider;
    private readonly List<Action<IClientBuilder>> _configurator;
    private IClusterClient? _clusterClientImplementation;

    public ClusterClientFactory(
        IServiceProvider serviceProvider,
        IEnumerable<Action<IClientBuilder>> configurator
    )
    {
        _loggerProvider = serviceProvider.GetRequiredService<ILoggerProvider>();
        _configurator = configurator.ToList();
    }

    private IClusterClient GetClusterClient()
    {
        IClusterClient BuildClusterClient()
        {
            var builder = new ClientBuilder();

            foreach (var item in _configurator) item(builder);

            builder
                .ConfigureLogging(logging => logging.AddProvider(_loggerProvider))
                .Configure<SerializationProviderOptions>(options =>
                {
                    options.FallbackSerializationProvider = typeof(NewtonsoftJsonExternalSerializer);
                });

            return builder.Build();
        }


        if (_clusterClientImplementation == null)
            _clusterClientImplementation = BuildClusterClient();

        if (!_clusterClientImplementation?.IsInitialized ?? true)
            _clusterClientImplementation = BuildClusterClient();

        return _clusterClientImplementation;
    }

    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
    }

    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
    }

    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidCompoundKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
    }

    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerCompoundKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
    }

    public Task<TGrainObserverInterface> CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        return ClusterClientImplementation.CreateObjectReference<TGrainObserverInterface>(obj);
    }

    public Task DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        return ClusterClientImplementation.DeleteObjectReference<TGrainObserverInterface>(obj);
    }

    public void BindGrainReference(IAddressable grain)
    {
        ClusterClientImplementation.BindGrainReference(grain);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Type grainInterfaceType, Guid grainPrimaryKey) where TGrainInterface : IGrain
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainInterfaceType, grainPrimaryKey);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Type grainInterfaceType, long grainPrimaryKey) where TGrainInterface : IGrain
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainInterfaceType, grainPrimaryKey);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Type grainInterfaceType, string grainPrimaryKey) where TGrainInterface : IGrain
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainInterfaceType, grainPrimaryKey);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension) where TGrainInterface : IGrain
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainInterfaceType, grainPrimaryKey, keyExtension);
    }

    public TGrainInterface GetGrain<TGrainInterface>(Type grainInterfaceType, long grainPrimaryKey, string keyExtension) where TGrainInterface : IGrain
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainInterfaceType, grainPrimaryKey, keyExtension);
    }

    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey)
    {
        return ClusterClientImplementation.GetGrain(grainInterfaceType, grainPrimaryKey);
    }

    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey)
    {
        return ClusterClientImplementation.GetGrain(grainInterfaceType, grainPrimaryKey);
    }

    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey)
    {
        return ClusterClientImplementation.GetGrain(grainInterfaceType, grainPrimaryKey);
    }

    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension)
    {
        return ClusterClientImplementation.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);
    }

    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension)
    {
        return ClusterClientImplementation.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
            
    }

    public IStreamProvider GetStreamProvider(string name)
    {
        return ClusterClientImplementation.GetStreamProvider(name);
    }

    public async Task Connect(Func<Exception, Task<bool>> retryFilter = null)
    {
        try
        {
            await ClusterClientImplementation.Connect(retryFilter);
        }
        catch
        {
            await DisposeWorker();
            throw;
        }
    }

    private async Task DisposeWorker()
    {
        if (_clusterClientImplementation != null)
        {
            await _clusterClientImplementation.DisposeAsync();
            _clusterClientImplementation = null;
        }
    }

    public async Task Close()
    {
        var client = ClusterClientImplementation;
        await client.Close();
        await DisposeWorker();
    }

    public Task AbortAsync()
    {
        return ClusterClientImplementation.AbortAsync();
    }

    public bool IsInitialized => ClusterClientImplementation.IsInitialized;

    public IServiceProvider ServiceProvider => ClusterClientImplementation.ServiceProvider;

    public IClusterClient ClusterClientImplementation => GetClusterClient();
}