using System.Linq.Expressions;
using Nyx.Data;

namespace Nyx.Orleans.Data;

public abstract class BaseEntityRepositoryQueryGrain<TModel, TDao> : BaseQueryGrain<TModel>
    where TDao : class
{
    private readonly IEntityRepository<TDao> _cloudResourceRepository;

    protected BaseEntityRepositoryQueryGrain(
        IEntityRepository<TDao> cloudResourceRepository
    )
    {
        _cloudResourceRepository = cloudResourceRepository;
    }
    
    protected override async Task<IEnumerable<TModel>> ExecuteFetchAll()
    {
        var queryParameters = GetQueryParameters();
        
        var resultDao = await _cloudResourceRepository.Query(q =>
        {
            q = CustomizeEntityQueryable(q);
            
            if (queryParameters.SearchString.Length != 0)
            {
                q = q.Where(BuildGenericSearchStringFilter(queryParameters.SearchString));
            }
            
            foreach (var filter in queryParameters.Filters.Select(filter => BuildFilterBasedOnModelField(filter.Field, filter.Values) ).ToArray())
            {
                q = q.Where(filter);
            }

            return q;
        });

        var result = new List<TModel>();
        
        foreach (var id in resultDao.ToList())
        {
            result.Add(await ConvertDaoToModel(id));
        }

        return result;
    }
    
    protected override async Task<IEnumerable<TModel>> ExecuteFetchPage(int page)
    {
        var queryParameters = GetQueryParameters();

        if (queryParameters.PageSize == 0)
        {
            throw new InvalidOperationException("Cannot fetch a page, without setting the PageSize query parameter.");
        }
        
        var resultDao = await _cloudResourceRepository.Query(q =>
        {
            q = CustomizeEntityQueryable(q);
            
            if (queryParameters.SearchString.Length != 0)
            {
                q = q.Where(BuildGenericSearchStringFilter(queryParameters.SearchString));
            }
            
            foreach (var filter in queryParameters.Filters.Select(filter => BuildFilterBasedOnModelField(filter.Field, filter.Values) ).ToArray())
            {
                q = q.Where(filter);
            }
            
            if (queryParameters.PageSize != 0)
            {
                q = q.Skip((page * queryParameters.PageSize))
                    .Take(queryParameters.PageSize);
            }

            return q;
        });

        var result = new List<TModel>();
        
        foreach (var id in resultDao.ToList())
        {
            result.Add(await ConvertDaoToModel(id));
        }

        return result;
    }

    protected virtual IQueryable<TDao> CustomizeEntityQueryable(IQueryable<TDao> queryable)
    {
        return queryable;
    }
    
    /// <summary>
    ///     Converts the DAO / entity objects retrieved from the entity repository.
    /// </summary>
    /// <param name="dao"></param>
    /// <returns></returns>
    protected abstract Task<TModel> ConvertDaoToModel(TDao dao);

    /// <summary>
    ///     Builds an expression filter based on the search string provided.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <returns></returns>
    protected abstract Expression<Func<TDao, bool>> BuildGenericSearchStringFilter(string searchTerm);

    /// <summary>
    ///     Builds an expression filter based on the field name and the values provided.
    /// </summary>
    /// <param name="field"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    protected abstract Expression<Func<TDao, bool>> BuildFilterBasedOnModelField(string field, string[] values);

    protected override Task<int> ExecuteCount()
    {
        var queryParameters = GetQueryParameters();
        
        return _cloudResourceRepository.Count(q =>
        {
            q = CustomizeEntityQueryable(q);
            
            if (queryParameters.SearchString.Length != 0)
            {
                q = q.Where(BuildGenericSearchStringFilter(queryParameters.SearchString));
            }

            return queryParameters.Filters.Select(filter => BuildFilterBasedOnModelField(filter.Field, filter.Values))
                .ToArray()
                .Aggregate(q, (current, filter) => current.Where(filter));
        });
    }
}