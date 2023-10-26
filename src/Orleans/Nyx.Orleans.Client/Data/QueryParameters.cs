namespace Nyx.Orleans.Data;


public record QueryParameters(
    List<QueryFilter> Filters,
    QueryResultOrder Order,
    string SearchString = "",
    int PageSize = 10
)
{
    public static readonly QueryParameters Default = new(new(), QueryResultOrder.Default);
}

public record QueryFilter(string Field, string[] Values);

public record QueryResultOrder(string Field, bool Ascending)
{
    public static readonly QueryResultOrder Default = new("", true);
}
