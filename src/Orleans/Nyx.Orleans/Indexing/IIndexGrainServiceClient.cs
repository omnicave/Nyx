using System.Threading.Tasks;
using Orleans;
using Orleans.Services;

namespace Shipbot.Common.Indexing;

public interface IIndexGrainServiceClient<TIndexGrainContract> : IGrainServiceClient<IIndexGrainService>
    where TIndexGrainContract : IIndexGrain, IGrain
{
    Task Index();
}