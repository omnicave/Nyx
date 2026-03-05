using Orleans.Services;

namespace Nyx.Orleans.Indexing;

public interface IIndexGrainServiceClient<TIndexGrainContract> : IGrainServiceClient<IIndexGrainService>
    where TIndexGrainContract : IIndexGrain, IGrain
{
    Task Index();
}