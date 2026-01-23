using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.Services.Interfaces;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ApiControllerBase
    {
        private readonly IAdminUserService _userService;

        public AdminUsersController(IAdminUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            var result = await _userService.GetUsersAsync(search, page, pageSize, currentUserId);
            return FromServiceResult(result);
        }

        [HttpPut("{userId}/admin")]
        public async Task<IActionResult> UpdateAdminRole(string userId, [FromBody] AdminUserRoleUpdateRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            var result = await _userService.UpdateAdminRoleAsync(userId, request, currentUserId);
            return FromServiceResult(result);
        }

        private IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Value);
            }

            var code = result.ErrorCode ?? "ServerError";
            var message = result.ErrorMessage ?? "An unexpected error occurred.";

            return code switch
            {
                "NotFound" => NotFoundError(message, code, result.ErrorData),
                "ValidationError" => BadRequestError(message, code, result.ErrorData),
                "Unauthorized" => UnauthorizedError(message, code, result.ErrorData),
                "Forbidden" => ForbiddenError(message, code, result.ErrorData),
                "Conflict" => ConflictError(message, code, result.ErrorData),
                _ => ServerError(message, code, result.ErrorData)
            };
        }
    }
}
