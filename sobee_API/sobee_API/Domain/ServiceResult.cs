namespace sobee_API.Domain;

public sealed record ServiceResult<T>(bool Success, T? Value, string? ErrorCode, string? ErrorMessage, object? ErrorData)
{
    public static ServiceResult<T> Ok(T value)
        => new(true, value, null, null, null);

    public static ServiceResult<T> Fail(string errorCode, string errorMessage, object? errorData = null)
        => new(false, default, errorCode, errorMessage, errorData);
}
