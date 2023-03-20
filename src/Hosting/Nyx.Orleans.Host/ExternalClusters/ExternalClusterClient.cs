using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Nyx.Orleans.Host;

internal class ExternalClusterClient : IClusterClient, IHostedService
{
    private readonly ServiceCollection _serviceCollection;
    private IServiceProvider? _serviceProvider;
    private IClusterClient? _clusterClientImplementation;

    public ExternalClusterClient(
        IServiceProvider parentServiceProvider,
        IEnumerable<Action<IClientBuilder>> configurator
    )
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddSingleton(parentServiceProvider.GetRequiredService<ILoggerProvider>());
        _serviceCollection.AddSingleton(parentServiceProvider.GetRequiredService<ILoggerFactory>());
        _serviceCollection.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        var builder = new ClientBuilder(_serviceCollection);

        foreach (var item in configurator) 
            item(builder);

        builder
            .Services.AddSerializer(
                serializerBuilder => serializerBuilder.AddNewtonsoftJsonSerializer(type => true)
            );
    }

    private IClusterClient GetClusterClient()
    {
        IClusterClient BuildClusterClient()
        {
            _serviceProvider ??= _serviceCollection.BuildServiceProvider();
            return _serviceProvider.GetRequiredService<IClusterClient>();
        }
        
        if (_clusterClientImplementation == null)
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

    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string grainClassNamePrefix) where TGrainInterface : IGrainWithGuidCompoundKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
    }

    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string grainClassNamePrefix) where TGrainInterface : IGrainWithIntegerCompoundKey
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);
    }

    public TGrainObserverInterface CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        return ClusterClientImplementation.CreateObjectReference<TGrainObserverInterface>(obj);
    }

    public void DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
    {
        ClusterClientImplementation.DeleteObjectReference<TGrainObserverInterface>(obj);
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

    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId) where TGrainInterface : IAddressable
    {
        return ClusterClientImplementation.GetGrain<TGrainInterface>(grainId);
    }

    public IAddressable GetGrain(GrainId grainId)
    {
        return ClusterClientImplementation.GetGrain(grainId);
    }

    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType)
    {
        return ClusterClientImplementation.GetGrain(grainId, interfaceType);
    }

    public IServiceProvider ServiceProvider => ClusterClientImplementation.ServiceProvider;

    private IClusterClient ClusterClientImplementation => GetClusterClient();
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var clusterClient = GetClusterClient();
        if (clusterClient is IHostedService clusterClientHostedService)
            return clusterClientHostedService.StartAsync(cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var clusterClient = GetClusterClient();
        if (clusterClient is IHostedService clusterClientHostedService)
            return clusterClientHostedService.StopAsync(cancellationToken);

        return Task.CompletedTask;
    }
}