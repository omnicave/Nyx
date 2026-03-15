namespace Nyx.Orleans.WebApi;

public static class ResultExtensions {

    public static Result<TPayload> ToSuccessfulResult<TPayload>(this TPayload payload) 
        => Result<TPayload>.Success(payload);
}

[GenerateSerializer]
public record Result<TPayload>(
    [property: Id(0)] bool Successful,
    [property: Id(1)] TPayload? Data,
    [property: Id(2)] Error? Error
)
{
    public static Result<TPayload> Success(TPayload data) => new(true, data, Error.None);
    public static Result<TPayload> Failure(Error error) => new(false, default, error);
    public static Result<TPayload> Failure(int code, string description = "") => new(false, default, new Error(code, description));
}

[GenerateSerializer]
public record CollectionResult<TPayload>(
    [property: Id(0)] bool Successful,
    [property: Id(1)] IEnumerable<TPayload> Data,
    [property: Id(2)] Error? Error
)
{
    public static CollectionResult<TPayload> Success(IEnumerable<TPayload> data) => new(true, data, Error.None);
    public static CollectionResult<TPayload> Failure(Error error) => new(false, Array.Empty<TPayload>(), error);   
    public static CollectionResult<TPayload> Failure(int code, string description = "") => new(false, Array.Empty<TPayload>(), new Error(code, description));   
}

[GenerateSerializer]
public record PagedResult<TPayload>(
    [property: Id(0)] bool Successful,
    [property: Id(1)] int Page,
    [property: Id(2)] int Count,
    [property: Id(3)] IEnumerable<TPayload> Data,
    [property: Id(4)] Error? Error
)
{
    public static PagedResult<TPayload> Success(int page, int count, IEnumerable<TPayload> data) => new(true, page, count, data, Error.None);
    public static PagedResult<TPayload> Failure(Error error) => new(false, 0, 0, Array.Empty<TPayload>(), error);   
    public static PagedResult<TPayload> Failure(int code, string description = "") => new(false, 0, 0, Array.Empty<TPayload>(), new Error(code, description));   
}

[GenerateSerializer]
public record Error(
    [property: Id(0)] int Code,
    [property: Id(1)] string Description
)
{
    public static readonly Error None = new(0, string.Empty);
}
