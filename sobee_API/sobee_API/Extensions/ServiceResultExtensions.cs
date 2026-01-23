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

        var code = result.ErrorCode ?? "ServerError";
        var message = result.ErrorMessage ?? "An unexpected error occurred.";
        var error = new ApiErrorResponse(message, code, result.ErrorData);

        return code switch
        {
            "NotFound" => controller.NotFound(error),
            "ValidationError" => controller.BadRequest(error),
            "InvalidPromo" => controller.BadRequest(error),
            "Unauthorized" => controller.Unauthorized(error),
            "Forbidden" => controller.StatusCode(StatusCodes.Status403Forbidden, error),
            "Conflict" => controller.Conflict(error),
            "InsufficientStock" => controller.Conflict(error),
            "InvalidStatusTransition" => controller.Conflict(error),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, error)
        };
    }
}
