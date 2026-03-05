using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;

namespace Shipbot.Common.Indexing;

public interface IIndexGrain : IGrainWithIntegerKey
{
    Task Index(GrainId grainReferenceGrainId);
}

public abstract class BaseIndexGrain<TGrain> : Grain, IGrainWithIntegerKey, IIndexGrain
    where TGrain : IGrain
{
    
    protected TGrain GetGrainToIndex(GrainId grainId) => GrainFactory.GetGrain<TGrain>(grainId);

    public abstract Task Index(GrainId grainReferenceGrainId);
}