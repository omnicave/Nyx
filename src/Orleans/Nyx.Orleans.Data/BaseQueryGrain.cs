namespace Nyx.Orleans.Data;

public abstract class BaseQueryGrain<TModel> : Grain, IQueryGrain<TModel>
{
    private bool _isLocked = false;
    private QueryParameters _queryParameters = QueryParameters.Default;

    public Task Configure(QueryParameters queryParameters)
    {
        if (_isLocked)
            throw new InvalidOperationException(
                "Cannot chane configuration because query already executed once (or more) and it's now locked.");
        
        if (_queryParameters != QueryParameters.Default)
            throw new InvalidOperationException("Cannot call QueryGrain<TModel>.Configure() more than once.");

        _queryParameters = queryParameters;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TModel>> FetchAll()
    {
        _isLocked = true;
        return ExecuteFetchAll();
    }

    public Task<IEnumerable<TModel>> FetchPage(int page)
    {
        _isLocked = true;
        return ExecuteFetchPage(page);
    }

    public Task<int> Count()
    {
        _isLocked = true;
        return ExecuteCount();
    }

    protected abstract Task<IEnumerable<TModel>> ExecuteFetchAll();
    protected abstract Task<IEnumerable<TModel>> ExecuteFetchPage(int page);
    protected abstract Task<int> ExecuteCount();

    protected QueryParameters GetQueryParameters() => _queryParameters;
}