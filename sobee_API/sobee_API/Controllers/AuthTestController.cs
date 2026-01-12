using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Auth;

namespace sobee_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthTestController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;

    public AuthTestController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    /// <summary>
    /// Get the current authenticated user's basic identity details.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);

        return Ok(new AuthTestMeResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Roles = roles
        });
    }

    /// <summary>
    /// Admin-only auth test endpoint.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new AuthTestAdminOnlyResponseDto { Ok = true, Message = "You are Admin." });
    }
}
