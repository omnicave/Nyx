using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nyx.Data
{
    public interface IDataOperationContextFactory
    {
        IDataOperationContext GetTransactionalOperationContext();
        IDataOperationContext GetBatchingOperationContext();
        IDataOperationContext GetSimpleOperationContext();
    }

    class DataOperationContextFactory : IDataOperationContextFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DataOperationContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDataOperationContext GetTransactionalOperationContext() =>
            new DataOperationContextWrapper<TransactionalDataOperationContext>(_serviceProvider);

        public IDataOperationContext GetBatchingOperationContext() =>
            new DataOperationContextWrapper<BatchingDataOperationContext>(_serviceProvider);

        public IDataOperationContext GetSimpleOperationContext() =>
            new DataOperationContextWrapper<SimpleDataOperationContext>(_serviceProvider);
    }

    public interface IDataOperationContext : IDisposable, IAsyncDisposable
    {
        DbSet<T> GetDbSet<T>() where T : class;

        Task Commit();
        
        Task Rollback();

        IDataChangeOperation BeginChangeOperation();
        
        IDataFetchOperation BeginFetchOperation();
    }

    public interface IDataFetchOperation : IAsyncDisposable
    {
    }

    public interface IDataChangeOperation : IAsyncDisposable
    {
    }

    public class DataOperationContextWrapper<T> : IDataOperationContext
        where T: IDataOperationContext
    {
        private readonly IServiceScope _scope;
        private readonly T _context;

        public DataOperationContextWrapper(IServiceProvider rootServiceProvider)
        {
            _scope = rootServiceProvider.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<T>();
        }

        public void Dispose()
        {
            _context.Dispose();
            _scope.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            _scope.Dispose();
        }

        public DbSet<T1> GetDbSet<T1>() where T1 : class
        {
            return _context.GetDbSet<T1>();
        }

        public Task Commit()
        {
            return _context.Commit();
        }

        public Task Rollback()
        {
            return _context.Rollback();
        }

        public IDataChangeOperation BeginChangeOperation()
        {
            return _context.BeginChangeOperation();
        }

        public IDataFetchOperation BeginFetchOperation()
        {
            return _context.BeginFetchOperation();
        }
    }

    /// <summary>
    ///     The SimpleDataOperationContext commits changes immediately.
    /// </summary>
    public class SimpleDataOperationContext : IDataOperationContext
    {
        private readonly ILogger<SimpleDataOperationContext> _log;
        private int _actionCount = 0;
        private int _rowUpdates = 0;
        private readonly RootDbContext _dbContext;

        public SimpleDataOperationContext(
            ILogger<SimpleDataOperationContext> log,
            RootDbContext dbContext
            )
        {
            _log = log;
            _dbContext = dbContext;
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
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public class TransactionalDataOperationContext : IDataOperationContext
    {
        private readonly ILogger<TransactionalDataOperationContext> _log;
        private readonly RootDbContext _dbContext;
        private readonly IDbContextTransaction _transaction;

        public TransactionalDataOperationContext(
            ILogger<TransactionalDataOperationContext> log,
            RootDbContext dbContext
        )
        {
            _log = log;
            _dbContext = dbContext;
            _transaction = dbContext.Database.BeginTransaction();

        }
        
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
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
        
        class TransactionalDataFetchOperation : IDataFetchOperation
        {
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }
    }

    public class BatchingDataOperationContext : IDataOperationContext
    {
        private readonly ILogger<BatchingDataOperationContext> _log;
        private readonly RootDbContext _dbContext;

        public BatchingDataOperationContext(
            ILogger<BatchingDataOperationContext> log,
            RootDbContext dbContext
            )
        {
            _log = log;
            _dbContext = dbContext;
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
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
    }
}