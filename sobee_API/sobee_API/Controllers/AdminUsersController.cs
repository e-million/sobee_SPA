using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Common;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _identityDb;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUsersController(ApplicationDbContext identityDb, UserManager<ApplicationUser> userManager)
        {
            _identityDb = identityDb;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page <= 0)
                return BadRequest(new ApiErrorResponse("page must be >= 1", "ValidationError"));

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new ApiErrorResponse("pageSize must be between 1 and 100", "ValidationError"));

            var query = _identityDb.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(term)) ||
                    (u.UserName != null && u.UserName.Contains(term)) ||
                    u.strFirstName.Contains(term) ||
                    u.strLastName.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.UserName,
                    u.strFirstName,
                    u.strLastName,
                    u.CreatedDate,
                    u.LastLoginDate,
                    u.LockoutEnd
                })
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var roleMappings = await _identityDb.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_identityDb.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
                .ToListAsync();

            var roleLookup = roleMappings
                .GroupBy(r => r.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.RoleName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            var items = users.Select(user =>
            {
                roleLookup.TryGetValue(user.Id, out var roles);
                roles ??= new List<string>();
                var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                return new AdminUserResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    FirstName = user.strFirstName,
                    LastName = user.strLastName,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    Roles = roles,
                    IsAdmin = isAdmin,
                    IsLocked = isLocked,
                    IsCurrentUser = !string.IsNullOrWhiteSpace(currentUserId) &&
                        string.Equals(currentUserId, user.Id, StringComparison.OrdinalIgnoreCase)
                };
            }).ToList();

            return Ok(new
            {
                page,
                pageSize,
                totalCount,
                items
            });
        }

        [HttpPut("{userId}/admin")]
        public async Task<IActionResult> UpdateAdminRole(string userId, [FromBody] AdminUserRoleUpdateRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new ApiErrorResponse("User not found.", "NotFound"));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!request.IsAdmin &&
                !string.IsNullOrWhiteSpace(currentUserId) &&
                string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ApiErrorResponse("You cannot remove your own admin access.", "ValidationError"));
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (request.IsAdmin && !isAdmin)
            {
                var addAdmin = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addAdmin.Succeeded)
                    return BadRequest(new ApiErrorResponse("Failed to add Admin role.", "ValidationError"));
            }

            if (!request.IsAdmin && isAdmin)
            {
                var removeAdmin = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!removeAdmin.Succeeded)
                    return BadRequest(new ApiErrorResponse("Failed to remove Admin role.", "ValidationError"));
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var response = new AdminUserResponse
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                FirstName = user.strFirstName,
                LastName = user.strLastName,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                Roles = roles.ToList(),
                IsAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase),
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                IsCurrentUser = !string.IsNullOrWhiteSpace(currentUserId) &&
                    string.Equals(currentUserId, user.Id, StringComparison.OrdinalIgnoreCase)
            };

            return Ok(response);
        }
    }
}
