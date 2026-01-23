namespace sobee_API.DTOs.Common;

public static class ErrorCodes
{
    public const string NotFound = "NotFound";
    public const string ValidationError = "ValidationError";
    public const string ValidationErrorUpper = "VALIDATION_ERROR";
    public const string InvalidPromo = "InvalidPromo";
    public const string Unauthorized = "Unauthorized";
    public const string Forbidden = "Forbidden";
    public const string Conflict = "Conflict";
    public const string EmailExists = "EMAIL_EXISTS";
    public const string InsufficientStock = "InsufficientStock";
    public const string InvalidStatusTransition = "InvalidStatusTransition";
    public const string ServerError = "ServerError";
    public const string ServerErrorUpper = "SERVER_ERROR";
    public const string RateLimited = "RATE_LIMITED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        NotFound,
        ValidationError,
        ValidationErrorUpper,
        InvalidPromo,
        Unauthorized,
        Forbidden,
        Conflict,
        EmailExists,
        InsufficientStock,
        InvalidStatusTransition,
        ServerError,
        ServerErrorUpper,
        RateLimited
    };
}
