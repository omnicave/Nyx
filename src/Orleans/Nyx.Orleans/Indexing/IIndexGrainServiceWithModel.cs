using Orleans.Services;

namespace Nyx.Orleans.Indexing;

public interface IIndexGrainService : IGrainService
{
    Task Index(GrainId callingGrainId);
}

public interface IIndexGrainService<TGrainContract> : IIndexGrainService
    where TGrainContract : IIndexGrain, IGrain
{
    
}