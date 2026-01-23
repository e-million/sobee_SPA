using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using sobee_API.DTOs.Admin;
using sobee_API.Services;
using Sobee.Domain.Identity;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class AdminUserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_InvalidPage_ReturnsValidationError()
    {
        var service = new AdminUserService(new FakeAdminUserRepository(), FakeUserManager.Create());

        var result = await service.GetUsersAsync(null, page: 0, pageSize: 20, currentUserId: null);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedResults()
    {
        var repo = new FakeAdminUserRepository();
        repo.AddUser(new AdminUserRecord("user-1", "user1@demo.com", "user1", "A", "One", DateTime.UtcNow.AddDays(-2), null, null), new[] { "Admin" });
        repo.AddUser(new AdminUserRecord("user-2", "user2@demo.com", "user2", "B", "Two", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, null), new[] { "User" });

        var service = new AdminUserService(repo, FakeUserManager.Create());

        var result = await service.GetUsersAsync(null, page: 1, pageSize: 10, currentUserId: "user-2");

        result.Success.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().ContainSingle(u => u.Id == "user-2" && u.IsCurrentUser);
        result.Value.Items.Should().ContainSingle(u => u.Id == "user-1" && u.IsAdmin);
    }

    [Fact]
    public async Task UpdateAdminRoleAsync_UserNotFound_ReturnsNotFound()
    {
        var service = new AdminUserService(new FakeAdminUserRepository(), FakeUserManager.Create());

        var result = await service.UpdateAdminRoleAsync("missing", new AdminUserRoleUpdateRequest { IsAdmin = true }, currentUserId: null);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task UpdateAdminRoleAsync_RemovingSelfAdmin_ReturnsValidationError()
    {
        var userManager = FakeUserManager.Create();
        userManager.AddUser(new ApplicationUser { Id = "user-1", Email = "user1@demo.com" }, "Admin");
        var service = new AdminUserService(new FakeAdminUserRepository(), userManager);

        var result = await service.UpdateAdminRoleAsync("user-1", new AdminUserRoleUpdateRequest { IsAdmin = false }, currentUserId: "user-1");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task UpdateAdminRoleAsync_AddsAdminRole()
    {
        var userManager = FakeUserManager.Create();
        userManager.AddUser(new ApplicationUser
        {
            Id = "user-1",
            Email = "user1@demo.com",
            strFirstName = "A",
            strLastName = "One"
        });

        var service = new AdminUserService(new FakeAdminUserRepository(), userManager);

        var result = await service.UpdateAdminRoleAsync("user-1", new AdminUserRoleUpdateRequest { IsAdmin = true }, currentUserId: "other");

        result.Success.Should().BeTrue();
        result.Value!.Roles.Should().Contain("Admin");
        result.Value.Roles.Should().Contain("User");
        result.Value.IsAdmin.Should().BeTrue();
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        private readonly List<AdminUserRecord> _users = new();
        private readonly List<AdminUserRoleRecord> _roles = new();

        public void AddUser(AdminUserRecord user, IEnumerable<string> roles)
        {
            _users.Add(user);
            foreach (var role in roles)
            {
                _roles.Add(new AdminUserRoleRecord(user.Id, role));
            }
        }

        public Task<(IReadOnlyList<AdminUserRecord> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize)
        {
            IEnumerable<AdminUserRecord> query = _users;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (u.UserName != null && u.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (u.FirstName != null && u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (u.LastName != null && u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var total = query.Count();
            query = query
                .OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return Task.FromResult(((IReadOnlyList<AdminUserRecord>)query.ToList(), total));
        }

        public Task<IReadOnlyList<AdminUserRoleRecord>> GetUserRolesAsync(IReadOnlyList<string> userIds)
        {
            IReadOnlyList<AdminUserRoleRecord> roles = _roles.Where(r => userIds.Contains(r.UserId)).ToList();
            return Task.FromResult(roles);
        }

        public Task<IReadOnlyList<AdminUserProfileRecord>> GetUsersByIdsAsync(IReadOnlyList<string> userIds)
        {
            IReadOnlyList<AdminUserProfileRecord> profiles = _users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new AdminUserProfileRecord(u.Id, u.Email, u.FirstName, u.LastName))
                .ToList();
            return Task.FromResult(profiles);
        }

        public Task<int> GetUserCountBeforeAsync(DateTime start)
            => Task.FromResult(0);

        public Task<IReadOnlyList<DateTime>> GetUserRegistrationsAsync(DateTime start, DateTime end)
            => Task.FromResult<IReadOnlyList<DateTime>>(Array.Empty<DateTime>());
    }

    private sealed class FakeUserManager : UserManager<ApplicationUser>
    {
        private readonly Dictionary<string, ApplicationUser> _users = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _roles = new(StringComparer.OrdinalIgnoreCase);

        private FakeUserManager()
            : base(
                new FakeUserStore(),
                new OptionsWrapper<IdentityOptions>(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                new NullLogger<UserManager<ApplicationUser>>())
        {
        }

        public static FakeUserManager Create()
            => new();

        public void AddUser(ApplicationUser user, params string[] roles)
        {
            _users[user.Id] = user;
            _roles[user.Id] = new HashSet<string>(roles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public override Task<ApplicationUser?> FindByIdAsync(string userId)
            => Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

        public override Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            var result = _roles.TryGetValue(user.Id, out var roles) && roles.Contains(role);
            return Task.FromResult(result);
        }

        public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        {
            if (!_roles.TryGetValue(user.Id, out var roles))
            {
                roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _roles[user.Id] = roles;
            }

            roles.Add(role);
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role)
        {
            if (_roles.TryGetValue(user.Id, out var roles))
            {
                roles.Remove(role);
            }

            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IList<string>> GetRolesAsync(ApplicationUser user)
        {
            IList<string> roles = _roles.TryGetValue(user.Id, out var stored)
                ? stored.ToList()
                : new List<string>();
            return Task.FromResult(roles);
        }
    }

    private sealed class FakeUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationUser?>(null);
    }
}
