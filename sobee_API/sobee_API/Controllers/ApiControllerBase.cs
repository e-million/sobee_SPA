using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sobee_API.DTOs.Common;

namespace sobee_API.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected BadRequestObjectResult BadRequestError(string message, string? code = null, object? details = null)
            => BadRequest(new ApiErrorResponse(message, code, details));

        protected NotFoundObjectResult NotFoundError(string message, string? code = null, object? details = null)
            => NotFound(new ApiErrorResponse(message, code, details));

        protected ConflictObjectResult ConflictError(string message, string? code = null, object? details = null)
            => Conflict(new ApiErrorResponse(message, code, details));

        protected UnauthorizedObjectResult UnauthorizedError(string message, string? code = null, object? details = null)
            => Unauthorized(new ApiErrorResponse(message, code, details));

        protected ObjectResult ForbiddenError(string message, string? code = null, object? details = null)
            => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(message, code, details));

        protected ObjectResult ServerError(
            string message = "An unexpected error occurred.",
            string? code = "ServerError",
            object? details = null)
            => StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(message, code, details));
    }
}
