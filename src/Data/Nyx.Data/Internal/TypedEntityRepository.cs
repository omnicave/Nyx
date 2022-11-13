using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Nyx.Data.Internal
{
    public class TypedEntityRepository<T> : IEntityRepository<T>
        where T : class
    {
        private readonly IDataOperationContextFactory _dataOperationContextFactory;

        public TypedEntityRepository(IDataOperationContextFactory dataOperationContextFactory)
        {
            _dataOperationContextFactory = dataOperationContextFactory;
        }

        public async Task<T> Add(T item)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Add(item, context);
        }

        public async Task<T> Add(T item, IDataOperationContext doc)
        {
            await using (doc.BeginChangeOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var entity = await dbSet.AddAsync(item);
                return entity.Entity ?? throw new InvalidOperationException();
            }
        }

        public async ValueTask<T?> Find<TKey>(TKey id)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Find(id, context);
        }

        public async ValueTask<T?> Find<TKey>(TKey id, IDataOperationContext doc)
        {
            if (id == null) 
                throw new ArgumentNullException(nameof(id));
            
            await using (doc.BeginFetchOperation())
            {
                var dbSet = doc.GetDbSet<T>();
            
                // ReSharper disable once HeapView.PossibleBoxingAllocation
                var result = await dbSet.FindAsync(new object[] { id }, CancellationToken.None);
                return result;    
            }
        }

        public async Task<int> Count()
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Count(context);
        }

        public async Task<int> Count(Func<IQueryable<T>, IQueryable<T>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Count(filter, context);
        }

        public async Task<int> Count(IDataOperationContext context)
        {
            await using (context.BeginFetchOperation())
            {
                var dbSet = context.GetDbSet<T>();
                return await dbSet.CountAsync();
            }
        }

        public async Task<int> Count(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context)
        {
            await using (context.BeginFetchOperation())
            {
                var dbSet = context.GetDbSet<T>();
                var query = filter(dbSet.AsQueryable());
                return await query.CountAsync();
            }
        }

        public async Task<T> Update(T item)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Update(item, context);
        }

        public async Task<T> Update(T item, IDataOperationContext doc)
        {
            await using (doc.BeginChangeOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var entity = dbSet.Update(item);
                return entity?.Entity ?? throw new InvalidOperationException();    
            }
        }

        public async Task Upsert(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            await Upsert(item, match, updateIfMatching, context);
        }

        public async Task Upsert(T item, Expression<Func<T, object>> match, Expression<Func<T, T>> updateIfMatching,  IDataOperationContext doc)
        {
            await using (doc.BeginChangeOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                await dbSet.Upsert(item).On(match).WhenMatched(updateIfMatching).RunAsync();    
            }
            
        }

        public async Task<IEnumerable<T>> QueryAll()
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await QueryAll(context);
        }

        public async Task<IEnumerable<T>> QueryAll(IDataOperationContext context)
        {
            await using (context.BeginFetchOperation())
            {
                var dbSet = context.GetDbSet<T>();
                return await dbSet.ToArrayAsync();
            }
        }

        public async Task<IEnumerable<T>> Query(Func<IQueryable<T>, IQueryable<T>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Query(filter, context);
        }
        
        public async Task<IEnumerable<T>> Query( Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext context )
        {
            await using (context.BeginFetchOperation())
            {
                var dbSet = context.GetDbSet<T>();
                var query = filter(dbSet.AsQueryable());
                return await query.ToArrayAsync();
            }
        }

        public async Task<IEnumerable<TResult>> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await Query(filter, context);
        }

        public async Task<IEnumerable<TResult>> Query<TResult>( Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext context )
        {
            await using (context.BeginFetchOperation())
            {
                var dbSet = context.GetDbSet<T>();
                var query = dbSet.AsQueryable();
                var result = filter(query);
                return await result.ToArrayAsync();
            }
        }

        public async Task<T> QuerySingle(Func<IQueryable<T>, IQueryable<T>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await QuerySingle(filter, context);
        }

        public async Task<T> QuerySingle(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext doc)
        {
            await using (doc.BeginFetchOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var filtered = filter(dbSet.AsQueryable());
                return await filtered.FirstAsync();
            }
        }

        public async Task<TResult> QuerySingle<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await QuerySingle(filter, context);
        }

        public async Task<TResult> QuerySingle<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext doc)
        {
            await using (doc.BeginFetchOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var filtered = filter(dbSet.AsQueryable());
                return await filtered.FirstAsync();
            }
        }

        public async Task<T?> QuerySingleOrDefault(Func<IQueryable<T>, IQueryable<T>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await QuerySingleOrDefault(filter, context);
        }

        public async Task<T?> QuerySingleOrDefault(Func<IQueryable<T>, IQueryable<T>> filter, IDataOperationContext doc)
        {
            await using (doc.BeginFetchOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var filtered = filter(dbSet.AsQueryable());
                return await filtered.FirstOrDefaultAsync();
            }
        }

        public async Task<TResult?> QuerySingleOrDefault<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            return await QuerySingleOrDefault(filter, context);
        }

        public async Task<TResult?> QuerySingleOrDefault<TResult>(Func<IQueryable<T>, IQueryable<TResult>> filter, IDataOperationContext doc)
        {
            await using (doc.BeginFetchOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                var filtered = filter(dbSet.AsQueryable());
                return await filtered.FirstOrDefaultAsync();
            }
        }

        public async Task Delete(T item)
        {
            await using var context = _dataOperationContextFactory.GetSimpleOperationContext();
            await Delete(item, context);
        }
        
        public async Task Delete(T item, IDataOperationContext doc)
        {
            await using (doc.BeginChangeOperation())
            {
                var dbSet = doc.GetDbSet<T>();
                dbSet.Remove(item);
            }
            
        }
    }
}