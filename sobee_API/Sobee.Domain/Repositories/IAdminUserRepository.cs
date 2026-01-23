using Sobee.Domain.Identity;

namespace Sobee.Domain.Repositories;

public interface IAdminUserRepository
{
    Task<(IReadOnlyList<AdminUserRecord> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize);
    Task<IReadOnlyList<AdminUserRoleRecord>> GetUserRolesAsync(IReadOnlyList<string> userIds);
    Task<IReadOnlyList<AdminUserProfileRecord>> GetUsersByIdsAsync(IReadOnlyList<string> userIds);
    Task<int> GetUserCountBeforeAsync(DateTime start);
    Task<IReadOnlyList<DateTime>> GetUserRegistrationsAsync(DateTime start, DateTime end);
}

public sealed record AdminUserRecord(
    string Id,
    string? Email,
    string? UserName,
    string? FirstName,
    string? LastName,
    DateTime CreatedDate,
    DateTime? LastLoginDate,
    DateTimeOffset? LockoutEnd);

public sealed record AdminUserRoleRecord(string UserId, string RoleName);

public sealed record AdminUserProfileRecord(
    string Id,
    string? Email,
    string? FirstName,
    string? LastName);
