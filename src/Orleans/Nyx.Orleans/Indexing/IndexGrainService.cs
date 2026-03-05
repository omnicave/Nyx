using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Runtime.Services;

namespace Shipbot.Common.Indexing;

public class IndexGrainService<TIndexGrainContract> : GrainService, IIndexGrainService<TIndexGrainContract>
    where TIndexGrainContract : IIndexGrain, IGrain
{
    private readonly IServiceProvider _services;
    private readonly IGrainFactory _grainFactory;

    public IndexGrainService(
        IServiceProvider services,
        GrainId id,
        Silo silo,
        ILoggerFactory loggerFactory,
        IGrainFactory grainFactory
    ) : base(id, silo, loggerFactory)
    {
        _services = services;
        _grainFactory = grainFactory;
    }
    
    public Task Index(GrainId callingGrainId)
    {
        var indexGrain = _grainFactory.GetIndexGrain<TIndexGrainContract>();
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