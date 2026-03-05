namespace Nyx.Orleans.WebApi;

public static class ResultExtensions {

    public static Result<TPayload> ToSuccessfulResult<TPayload>(this TPayload payload) 
        => Result<TPayload>.Success(payload);
}


public record Result<TPayload>(
    bool Successful,
    TPayload? Data,
    Error? Error
)
{
    public static Result<TPayload> Success(TPayload data) => new(true, data, Error.None);
    public static Result<TPayload> Failure(Error error) => new(false, default, error);
    public static Result<TPayload> Failure(int code, string description = "") => new(false, default, new Error(code, description));
}

public record CollectionResult<TPayload>(
    bool Successful,
    IEnumerable<TPayload> Data,
    Error? Error
)
{
    public static CollectionResult<TPayload> Success(IEnumerable<TPayload> data) => new(true, data, Error.None);
    public static CollectionResult<TPayload> Failure(Error error) => new(false, Array.Empty<TPayload>(), error);   
    public static CollectionResult<TPayload> Failure(int code, string description = "") => new(false, Array.Empty<TPayload>(), new Error(code, description));   
}

public record PagedResult<TPayload>(
    bool Successful,
    int Page,
    int Count,
    IEnumerable<TPayload> Data,
    Error? Error
)
{
    public static PagedResult<TPayload> Success(int page, int count, IEnumerable<TPayload> data) => new(true, page, count, data, Error.None);
    public static PagedResult<TPayload> Failure(Error error) => new(false, 0, 0, Array.Empty<TPayload>(), error);   
    public static PagedResult<TPayload> Failure(int code, string description = "") => new(false, 0, 0, Array.Empty<TPayload>(), new Error(code, description));   
}

public record Error(
    int Code,
    string Description
)
{
    public static readonly Error None = new(0, string.Empty);
}