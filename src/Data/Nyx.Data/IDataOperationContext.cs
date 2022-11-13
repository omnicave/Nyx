using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        
        IEntityRepository Entities { get; }
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

        public IEntityRepository Entities => _context.Entities;
    }
}