namespace Nyx.Orleans.Indexing;

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