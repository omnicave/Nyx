using System;
using Orleans;
using Orleans.Hosting;
using Shipbot.Common.Indexing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterIndex<TIndexGrain>(this IServiceCollection collection)
        where TIndexGrain : IIndexGrain
    {
        return collection.AddGrainService<IndexGrainService<TIndexGrain>>()
            .AddSingleton<IIndexGrainServiceClient<TIndexGrain>, IndexGrainServiceClient<TIndexGrain>>();
        
    }
}