using Microsoft.Extensions.Logging;
using Orleans.Runtime.Services;
using Orleans;

namespace Nyx.Orleans.Indexing;

public class IndexGrainService<TIndexGrainContract>(
    IServiceProvider services,
    GrainId id,
    Silo silo,
    ILoggerFactory loggerFactory,
    IGrainFactory grainFactory)
    : GrainService(id, silo, loggerFactory), IIndexGrainService<TIndexGrainContract>
    where TIndexGrainContract : IIndexGrain, IGrain
{
    private readonly IServiceProvider _services = services;

    public Task Index(GrainId callingGrainId)
    {
        var indexGrain = grainFactory.GetIndexGrain<TIndexGrainContract>();
        indexGrain.Index(callingGrainId).Ignore();
        return Task.CompletedTask;
    }
}

public class IndexGrainServiceClient<TIndexGrainContract> 
    : GrainServiceClient<IIndexGrainService<TIndexGrainContract>>, IIndexGrainServiceClient<TIndexGrainContract>
    where TIndexGrainContract : IIndexGrain, IGrain
{
    public IndexGrainServiceClient(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
    private IIndexGrainService<TIndexGrainContract> GrainService => GetGrainService(CurrentGrainReference.GrainId);
    
    /// <summary>
    ///     Invoked by the grain implementing TGrainContract to get indexed by the indexer grain.
    /// </summary>
    /// <returns></returns>
    public Task Index()
    {
        GrainService.Index(CurrentGrainReference.GrainId).Ignore();
        return Task.CompletedTask;
    }
}
