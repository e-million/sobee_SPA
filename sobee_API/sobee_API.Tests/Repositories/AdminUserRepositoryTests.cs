using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Identity;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class AdminUserRepositoryTests
{
    [Fact]
    public async Task GetUsersAsync_SearchFiltersCorrectly()
    {
        using var context = new SqliteTestContext();
        context.AddUser(CreateUser("user-1", "alpha@example.com", "Alpha", "User"));
        context.AddUser(CreateUser("user-2", "beta@example.com", "Beta", "User"));

        var (users, total) = await context.Repository.GetUsersAsync("alpha", page: 1, pageSize: 10);

        total.Should().Be(1);
        users.Should().ContainSingle(u => u.Email == "alpha@example.com");
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsRoleMappings()
    {
        using var context = new SqliteTestContext();
        context.AddRole(new IdentityRole("Admin") { Id = "role-admin" });
        context.AddUser(CreateUser("user-1", "alpha@example.com", "Alpha", "User"));
        context.AddUserRole("user-1", "role-admin");

        var roles = await context.Repository.GetUserRolesAsync(new[] { "user-1" });

        roles.Should().ContainSingle(r => r.UserId == "user-1" && r.RoleName == "Admin");
    }

    [Fact]
    public async Task GetUsersByIdsAsync_ReturnsProfiles()
    {
        using var context = new SqliteTestContext();
        context.AddUser(CreateUser("user-1", "alpha@example.com", "Alpha", "User"));
        context.AddUser(CreateUser("user-2", "beta@example.com", "Beta", "User"));

        var users = await context.Repository.GetUsersByIdsAsync(new[] { "user-2" });

        users.Should().ContainSingle(u => u.Id == "user-2");
    }

    [Fact]
    public async Task GetUserCountBeforeAsync_ReturnsCount()
    {
        using var context = new SqliteTestContext();
        context.AddUser(CreateUser("user-1", "alpha@example.com", "Alpha", "User", created: DateTime.UtcNow.AddDays(-2)));
        context.AddUser(CreateUser("user-2", "beta@example.com", "Beta", "User", created: DateTime.UtcNow.AddDays(-1)));

        var count = await context.Repository.GetUserCountBeforeAsync(DateTime.UtcNow.AddDays(-1.5));

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetUserRegistrationsAsync_ReturnsDates()
    {
        using var context = new SqliteTestContext();
        var start = DateTime.UtcNow.AddDays(-3);
        var end = DateTime.UtcNow.AddDays(1);
        context.AddUser(CreateUser("user-1", "alpha@example.com", "Alpha", "User", created: DateTime.UtcNow.AddDays(-2)));

        var dates = await context.Repository.GetUserRegistrationsAsync(start, end);

        dates.Should().ContainSingle();
    }

    private static ApplicationUser CreateUser(
        string id,
        string email,
        string firstName,
        string lastName,
        DateTime? created = null)
        => new()
        {
            Id = id,
            Email = email,
            UserName = email,
            strFirstName = firstName,
            strLastName = lastName,
            CreatedDate = created ?? DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public ApplicationDbContext DbContext { get; }
        public AdminUserRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new ApplicationDbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new AdminUserRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public ApplicationUser AddUser(ApplicationUser user)
        {
            DbContext.Users.Add(user);
            DbContext.SaveChanges();
            return user;
        }

        public IdentityRole AddRole(IdentityRole role)
        {
            DbContext.Roles.Add(role);
            DbContext.SaveChanges();
            return role;
        }

        public void AddUserRole(string userId, string roleId)
        {
            DbContext.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = roleId
            });
            DbContext.SaveChanges();
        }
    }
}
