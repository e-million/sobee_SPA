using Microsoft.AspNetCore.Identity;
using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Common;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Identity;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(IAdminUserRepository userRepository, UserManager<ApplicationUser> userManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<ServiceResult<PagedResponse<AdminUserResponse>>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        string? currentUserId)
    {
        if (page <= 0)
        {
            return Validation<PagedResponse<AdminUserResponse>>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<PagedResponse<AdminUserResponse>>("pageSize must be between 1 and 100", new { pageSize });
        }

        var (users, totalCount) = await _userRepository.GetUsersAsync(search, page, pageSize);
        var userIds = users.Select(u => u.Id).ToList();
        var roleMappings = await _userRepository.GetUserRolesAsync(userIds);

        var roleLookup = roleMappings
            .GroupBy(r => r.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RoleName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList(),
                StringComparer.OrdinalIgnoreCase);

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
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                Roles = roles,
                IsAdmin = isAdmin,
                IsLocked = isLocked,
                IsCurrentUser = !string.IsNullOrWhiteSpace(currentUserId)
                    && string.Equals(currentUserId, user.Id, StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();

        return ServiceResult<PagedResponse<AdminUserResponse>>.Ok(new PagedResponse<AdminUserResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    public async Task<ServiceResult<AdminUserResponse>> UpdateAdminRoleAsync(
        string userId,
        AdminUserRoleUpdateRequest request,
        string? currentUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound<AdminUserResponse>("User not found.", null);
        }

        if (!request.IsAdmin &&
            !string.IsNullOrWhiteSpace(currentUserId) &&
            string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return Validation<AdminUserResponse>("You cannot remove your own admin access.", null);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        if (request.IsAdmin && !isAdmin)
        {
            var addAdmin = await _userManager.AddToRoleAsync(user, "Admin");
            if (!addAdmin.Succeeded)
            {
                return Validation<AdminUserResponse>("Failed to add Admin role.", null);
            }
        }

        if (!request.IsAdmin && isAdmin)
        {
            var removeAdmin = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!removeAdmin.Succeeded)
            {
                return Validation<AdminUserResponse>("Failed to remove Admin role.", null);
            }
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
            IsCurrentUser = !string.IsNullOrWhiteSpace(currentUserId)
                && string.Equals(currentUserId, user.Id, StringComparison.OrdinalIgnoreCase)
        };

        return ServiceResult<AdminUserResponse>.Ok(response);
    }

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);
}
