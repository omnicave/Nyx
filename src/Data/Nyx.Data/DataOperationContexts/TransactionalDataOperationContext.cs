using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Nyx.Data.Internal;

namespace Nyx.Data;

public class TransactionalDataOperationContext : IDataOperationContext
{
    private readonly ILogger<TransactionalDataOperationContext> _log;
    private readonly IServiceProvider _serviceProvider;
    private readonly RootDbContext _dbContext;
    private readonly IDbContextTransaction _transaction;
    private readonly EntityRepository _entities;

    public TransactionalDataOperationContext(
        ILogger<TransactionalDataOperationContext> log,
        IServiceProvider serviceProvider,
        RootDbContext dbContext
    )
    {
        _log = log;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _transaction = dbContext.Database.BeginTransaction();
        _entities = new EntityRepository(this, serviceProvider);
    }
        
    public void Dispose()
    {
        _entities.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        
        return ValueTask.CompletedTask;
    }

    public DbSet<T> GetDbSet<T>() where T : class => _dbContext.Set<T>();
    public Task Commit()
    {
        _dbContext.SaveChangesAsync();
        return _transaction.CommitAsync();
    }

    public Task Rollback()
    {
        return _transaction.RollbackAsync();
    }

    public IDataChangeOperation BeginChangeOperation()
    {
        return new TransactionalDataChangeOperation();
    }

    class TransactionalDataChangeOperation : IDataChangeOperation
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    public IDataFetchOperation BeginFetchOperation()
    {
        return new TransactionalDataFetchOperation();
    }

    public IEntityRepository Entities => _entities;
    
    public IEntityRepository GetEntityRepository() => _entities;


    class TransactionalDataFetchOperation : IDataFetchOperation
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}