using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class GuestSessionRepositoryTests
{
    [Fact]
    public async Task FindValidAsync_ReturnsSession()
    {
        using var context = new SqliteTestContext();
        context.AddSession(CreateSession("session-1", "secret", DateTime.UtcNow.AddHours(1)));

        var result = await context.Repository.FindValidAsync("session-1", "secret", DateTime.UtcNow);

        result.Should().NotBeNull();
        result!.SessionId.Should().Be("session-1");
    }

    [Fact]
    public async Task FindByIdAsync_ReturnsSession()
    {
        using var context = new SqliteTestContext();
        context.AddSession(CreateSession("session-2", "secret", DateTime.UtcNow.AddHours(1)));

        var result = await context.Repository.FindByIdAsync("session-2", track: false);

        result.Should().NotBeNull();
        result!.Secret.Should().Be("secret");
    }

    [Fact]
    public async Task RemoveAsync_RemovesSession()
    {
        using var context = new SqliteTestContext();
        var session = context.AddSession(CreateSession("session-3", "secret", DateTime.UtcNow.AddHours(1)));

        await context.Repository.RemoveAsync(session);
        await context.Repository.SaveChangesAsync();

        var result = await context.Repository.FindByIdAsync("session-3");
        result.Should().BeNull();
    }

    private static GuestSession CreateSession(string id, string secret, DateTime expiresAt)
        => new()
        {
            SessionId = id,
            Secret = secret,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public GuestSessionRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new GuestSessionRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public GuestSession AddSession(GuestSession session)
        {
            DbContext.GuestSessions.Add(session);
            DbContext.SaveChanges();
            return session;
        }
    }
}
