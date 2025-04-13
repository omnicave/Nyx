namespace Nyx.WebApi.Minimal.Dto;

public record CollectionResult<TItem>(
    IEnumerable<TItem> Items,
    int Page,
    int TotalCount
);

public record ErrorResult(int Code, string Message)
{
    public static ErrorResult Success => new ErrorResult(200, "Success");
}