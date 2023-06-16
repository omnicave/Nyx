using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Nyx.Data;

public interface IDataOperationContext : IDisposable, IAsyncDisposable
{
    DbSet<T> GetDbSet<T>() where T : class;

    Task Commit();
        
    Task Rollback();

    IDataChangeOperation BeginChangeOperation();
        
    IDataFetchOperation BeginFetchOperation();
        
    [Obsolete("Use GetEntityRepository() instead.")]
    IEntityRepository Entities { get; }

    IEntityRepository GetEntityRepository();
}

/// <summary>
///     Describes a session where data will be fetched from the provider.
/// </summary>
public interface IDataFetchOperation : IAsyncDisposable
{
}

/// <summary>
///     Describes a session where data will be changed.
/// </summary>
public interface IDataChangeOperation : IAsyncDisposable
{
}