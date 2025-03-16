namespace Nyx.Orleans.Data;


[GenerateSerializer]
[Alias("Nyx.Orleans.Data.QueryParameters")]
public record QueryParameters(
    [property: Id(0)] List<QueryFilter> Filters,
    [property: Id(1)] QueryResultOrder Order,
    [property: Id(2)] string SearchString = "",
    [property: Id(3)] int PageSize = 10
)
{
    public static readonly QueryParameters Default = new(new(), QueryResultOrder.Default);
}

[GenerateSerializer]
[Alias("Nyx.Orleans.Data.QueryFilter")]
public record QueryFilter(
    [property: Id(10)] string Field, 
    [property: Id(11)] string[] Values);

[GenerateSerializer]
[Alias("Nyx.Orleans.Data.QueryResultOrder")]
public record QueryResultOrder(
    [property: Id(10)] string Field, 
    [property: Id(11)] bool Ascending)
{
    public static readonly QueryResultOrder Default = new("", true);
}
