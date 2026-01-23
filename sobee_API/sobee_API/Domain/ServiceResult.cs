namespace sobee_API.Domain;

public sealed record ServiceResult<T>(bool Success, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static ServiceResult<T> Ok(T value)
        => new(true, value, null, null);

    public static ServiceResult<T> Fail(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}
