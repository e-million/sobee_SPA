using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.DTOs.Auth;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        /// <summary>
        /// Get the authenticated user's profile claims and roles.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            var name = User.Identity?.Name;

            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
                .Select(c => c.Value)
                .Distinct()
                .ToArray();

            return Ok(new MeResponseDto
            {
                Name = name,
                Email = email,
                Roles = roles,
                Claims = User.Claims.Select(c => new MeClaimDto { Type = c.Type, Value = c.Value }).ToArray()
            });
        }
    }
}
