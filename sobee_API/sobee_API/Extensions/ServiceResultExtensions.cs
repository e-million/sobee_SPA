using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs.Common;

namespace sobee_API.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller)
    {
        if (result.Success)
        {
            return controller.Ok(result.Value);
        }

        var code = result.ErrorCode ?? ErrorCodes.ServerError;
        var message = result.ErrorMessage ?? "An unexpected error occurred.";
        var error = new ApiErrorResponse(message, code, result.ErrorData);

        return code switch
        {
            ErrorCodes.NotFound => controller.NotFound(error),
            ErrorCodes.ValidationError => controller.BadRequest(error),
            ErrorCodes.InvalidPromo => controller.BadRequest(error),
            ErrorCodes.Unauthorized => controller.Unauthorized(error),
            ErrorCodes.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, error),
            ErrorCodes.Conflict => controller.Conflict(error),
            ErrorCodes.InsufficientStock => controller.Conflict(error),
            ErrorCodes.InvalidStatusTransition => controller.Conflict(error),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, error)
        };
    }
}
