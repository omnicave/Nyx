using System;
using Orleans;

namespace Shipbot.Common.Indexing;

public static class GrainFactoryExtensions
{
    public static TGrainInterface GetIndexGrain<TGrainInterface>(this IGrainFactory grainFactory)
        where TGrainInterface : IIndexGrain
    {
        return grainFactory.GetGrain<TGrainInterface>(0);
    }
}