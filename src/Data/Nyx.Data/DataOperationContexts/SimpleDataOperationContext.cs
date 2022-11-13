using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nyx.Data.Internal;

namespace Nyx.Data;

/// <summary>
///     The SimpleDataOperationContext commits changes immediately.
/// </summary>
public class SimpleDataOperationContext : IDataOperationContext
{
    private readonly ILogger<SimpleDataOperationContext> _log;
    private int _actionCount = 0;
    private int _rowUpdates = 0;
    private readonly RootDbContext _dbContext;
    private readonly EntityRepository _entities;

    public SimpleDataOperationContext(
        ILogger<SimpleDataOperationContext> log,
        IServiceProvider serviceProvider,
        RootDbContext dbContext
    )
    {
        _log = log;
        _dbContext = dbContext;
        _entities = new EntityRepository(this, serviceProvider);
    }

    public DbSet<T> GetDbSet<T>() where T : class => _dbContext.Set<T>();
    public Task Commit()
    {
        return Task.CompletedTask;
    }

    public Task Rollback()
    {
        return Task.CompletedTask;
    }

    public IDataChangeOperation BeginChangeOperation()
    {
        _actionCount += 1;
        return new SimpleDataChangeOperation(this);
    }

    public IDataFetchOperation BeginFetchOperation()
    {
        _actionCount += 1;
        return new SimpleDataFetchOperation();
    }

    public IEntityRepository Entities => _entities;

    class SimpleDataChangeOperation : IDataChangeOperation
    {
        private readonly SimpleDataOperationContext _ctx;
        private int _changeCount;

        public SimpleDataChangeOperation(SimpleDataOperationContext ctx)
        {
            _ctx = ctx;
        }

        public async ValueTask DisposeAsync()
        {
            _changeCount = await _ctx._dbContext.SaveChangesAsync();
            Interlocked.Add(ref _ctx._rowUpdates, _changeCount);
        }
    }

    class SimpleDataFetchOperation : IDataFetchOperation
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    public void Dispose()
    {
        if (_dbContext.ChangeTracker.HasChanges())
        {
            _log.LogWarning("Disposing unit of work, but db context has pending changes.");
        }
        
        _entities.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}