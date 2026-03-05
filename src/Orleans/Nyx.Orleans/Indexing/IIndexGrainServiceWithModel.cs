using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Services;

namespace Shipbot.Common.Indexing;

public interface IIndexGrainService : IGrainService
{
    Task Index(GrainId callingGrainId);
}

public interface IIndexGrainService<TGrainContract> : IIndexGrainService
    where TGrainContract : IIndexGrain, IGrain
{
    
}