using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Nyx.Data
{
    public interface IEntityRepository
    {
        Task<T> Add<T>(T item) where T : class;
        ValueTask<T?> Find<TKey, T>(TKey id) where T : class;
        Task<int> Count<T>() where T : class;
        Task<int> Count<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class;
        Task<IEnumerable<T>> Query<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class;
        Task<IEnumerable<TResult>> Query<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter) 
            where T : class;
        Task<IEnumerable<T>> QueryAll<T>() where T : class;
        Task<T> QuerySingle<T>(Func<IQueryable<T>, IQueryable<T>> filter) where T : class;
        Task<TResult> QuerySingle<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter) 
            where T : class;
        Task<T?> QuerySingleOrDefault<T>(Func<IQueryable<T>, IQueryable<T>> filter) 
            where T : class;
        Task<TResult?> QuerySingleOrDefault<T, TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter)
            where T : class;

        Task<T> Update<T>(T item)
            where T : class;
        Task Upsert<T>(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching)
            where T : class;
        Task Delete<T>(T item)
            where T : class;
    }
    
    public interface IEntityRepository<T>
        where T: class
    {
        Task<T> Add(T item);
        Task<T> Add(T item, IDataOperationContext doc);

        ValueTask<T?> Find<TKey>(TKey id);
        ValueTask<T?> Find<TKey>(TKey id, IDataOperationContext context);
        
        Task<int> Count();
        Task<int> Count(Func<IQueryable<T>, IQueryable<T>> filter);
        Task<int> Count(IDataOperationContext context);
        Task<int> Count(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context);
        
        Task<IEnumerable<T>> QueryAll();
        Task<IEnumerable<T>> QueryAll(IDataOperationContext context);
        
        Task<IEnumerable<T>> Query(Func<IQueryable<T>, IQueryable<T>> filter);
        Task<IEnumerable<T>> Query(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context);
        
        Task<IEnumerable<TResult>> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter);
        Task<IEnumerable<TResult>> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext context);
        
        Task<T> QuerySingle(Func<IQueryable<T>, IQueryable<T>> filter);
        Task<T> QuerySingle(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context);
        
        
        Task<TResult> QuerySingle<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter);
        Task<TResult> QuerySingle<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext context);
        
        Task<T?> QuerySingleOrDefault(Func<IQueryable<T>, IQueryable<T>> filter);
        Task<T?> QuerySingleOrDefault(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context);
        
        Task<TResult?> QuerySingleOrDefault<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter);
        Task<TResult?> QuerySingleOrDefault<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext context);
        
        Task<T> Update(T item);
        Task<T> Update(T item, IDataOperationContext context);
        

        Task Upsert(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching);
        Task Upsert(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching, IDataOperationContext context);
        
        Task Delete(T item);
        Task Delete(T item, IDataOperationContext context);
    }
}