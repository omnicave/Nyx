using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Nyx.Data.Internal;

internal class EntityRepository : IEntityRepository, IDisposable
{
    private readonly IDataOperationContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _entityRepositoryMap = new();

    public EntityRepository(IDataOperationContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    protected IEntityRepository<T> GetRepository<T>() where T : class
    {
        var i = _entityRepositoryMap.GetOrAdd(
            typeof(T), 
            static (type, sp) => sp.GetRequiredService<IEntityRepository<T>>(),
            _serviceProvider
        );

        return (IEntityRepository<T>)i;
    }

    public Task<T> Add<T>(T item) where T : class 
        => GetRepository<T>().Add(item, _context);

    public ValueTask<T?> Find<TKey, T>(TKey id) where T : class 
        => GetRepository<T>().Find(id, _context);

    public Task<int> Count<T>()  where T : class => GetRepository<T>().Count(_context);

    public Task<int> Count<T>(Func<IQueryable<T>, IQueryable<T>> filter)  where T : class => GetRepository<T>().Count(filter, _context);

    public Task<IEnumerable<T>> Query<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class
    {
        return GetRepository<T>().Query(filter, _context);
    }

    public Task<IEnumerable<TResult>> Query<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter) where T : class
    {
        return GetRepository<T>().Query<TResult>(filter, _context);
    }

    public Task<IEnumerable<T>> QueryAll<T>() where T : class
    {
        return GetRepository<T>().QueryAll(_context);
    }

    public Task<T> QuerySingle<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class
    {
        return GetRepository<T>().QuerySingle(filter, _context);
    }

    public Task<TResult> QuerySingle<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter) where T : class
    {
        return GetRepository<T>().QuerySingle<TResult>(filter, _context);
    }

    public Task<T?> QuerySingleOrDefault<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class
    {
        return GetRepository<T>().QuerySingleOrDefault(filter, _context);
    }

    public Task<TResult?> QuerySingleOrDefault<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter) where T : class
    {
        return GetRepository<T>().QuerySingleOrDefault<TResult>(filter, _context);
    }

    public Task<T> Update<T>(T item) where T : class
    {
        return GetRepository<T>().Update(item, _context);
    }

    public Task Upsert<T>(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching) where T : class
    {
        return GetRepository<T>().Upsert(item, match, updateIfMatching, _context);
    }

    public Task Delete<T>(T item) where T : class
    {
        return GetRepository<T>().Delete(item, _context);
    }

    public void Dispose()
    {
        _entityRepositoryMap.Clear();
    }
}