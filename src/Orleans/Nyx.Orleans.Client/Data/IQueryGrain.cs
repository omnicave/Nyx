namespace Nyx.Orleans.Data;

public interface IQueryGrain<TModel> : IGrainWithGuidKey
{
    Task Configure(QueryParameters queryParameters);
    
    /// <summary>
    ///     Executes the query and returns all results.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<TModel>> FetchAll();
    
    /// <summary>
    ///     Executes the query and returns the results for the specified page.
    /// </summary>
    /// <param name="page">Page index to fetch.</param>
    /// <param name="size">Number of records to return in page.</param>
    /// <returns></returns>
    Task<IEnumerable<TModel>> FetchPage(int page);

    Task<int> Count();
}

