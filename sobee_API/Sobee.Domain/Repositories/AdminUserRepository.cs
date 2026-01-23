using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;

namespace Sobee.Domain.Repositories;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly ApplicationDbContext _db;

    public AdminUserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<AdminUserRecord> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize)
    {
        var query = _db.Users.AsNoTracking();

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
            .Select(u => new AdminUserRecord(
                u.Id,
                u.Email,
                u.UserName,
                u.strFirstName,
                u.strLastName,
                u.CreatedDate,
                u.LastLoginDate,
                u.LockoutEnd))
            .ToListAsync();

        return (users, totalCount);
    }

    public async Task<IReadOnlyList<AdminUserRoleRecord>> GetUserRolesAsync(IReadOnlyList<string> userIds)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<AdminUserRoleRecord>();
        }

        var roles = await _db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_db.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new AdminUserRoleRecord(ur.UserId, r.Name ?? string.Empty))
            .ToListAsync();

        return roles;
    }

    public async Task<IReadOnlyList<AdminUserProfileRecord>> GetUsersByIdsAsync(IReadOnlyList<string> userIds)
    {
        if (userIds.Count == 0)
        {
            return Array.Empty<AdminUserProfileRecord>();
        }

        return await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new AdminUserProfileRecord(
                u.Id,
                u.Email,
                u.strFirstName,
                u.strLastName))
            .ToListAsync();
    }

    public async Task<int> GetUserCountBeforeAsync(DateTime start)
    {
        return await _db.Users
            .AsNoTracking()
            .CountAsync(u => u.CreatedDate < start);
    }

    public async Task<IReadOnlyList<DateTime>> GetUserRegistrationsAsync(DateTime start, DateTime end)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.CreatedDate >= start && u.CreatedDate <= end)
            .Select(u => u.CreatedDate)
            .ToListAsync();
    }
}
