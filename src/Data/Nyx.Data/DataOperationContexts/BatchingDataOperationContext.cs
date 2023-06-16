using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nyx.Data.Internal;

namespace Nyx.Data;

public class BatchingDataOperationContext : IDataOperationContext
{
    private readonly ILogger<BatchingDataOperationContext> _log;
    private readonly IServiceProvider _serviceProvider;
    private readonly RootDbContext _dbContext;
    private readonly EntityRepository _entities;

    public BatchingDataOperationContext(
        ILogger<BatchingDataOperationContext> log,
        IServiceProvider serviceProvider,
        RootDbContext dbContext
    )
    {
        _log = log;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _entities = new EntityRepository(this, serviceProvider);
    }

    public void Dispose()
    {
        _entities.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _entities.Dispose();
        
        return ValueTask.CompletedTask;
    }

    public DbSet<T> GetDbSet<T>() where T : class => _dbContext.Set<T>();

    public Task Commit()
    {
        return _dbContext.SaveChangesAsync();
    }

    public Task Rollback()
    {
        return Task.CompletedTask;
    }

    class BatchingDataChangeOperation : IDataChangeOperation
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
        
    class BatchingDataFetchOperation : IDataFetchOperation
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
        
    public IDataChangeOperation BeginChangeOperation()
    {
        return new BatchingDataChangeOperation();
    }

    public IDataFetchOperation BeginFetchOperation()
    {
        return new BatchingDataFetchOperation();
    }

    public IEntityRepository Entities => _entities;
    public IEntityRepository GetEntityRepository() => _entities;
}