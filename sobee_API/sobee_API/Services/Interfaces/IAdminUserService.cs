using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.DTOs.Common;

namespace sobee_API.Services.Interfaces;

public interface IAdminUserService
{
    Task<ServiceResult<PagedResponse<AdminUserResponse>>> GetUsersAsync(string? search, int page, int pageSize, string? currentUserId);
    Task<ServiceResult<AdminUserResponse>> UpdateAdminRoleAsync(string userId, AdminUserRoleUpdateRequest request, string? currentUserId);
}
