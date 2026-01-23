using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class SystemHealthRepositoryTests
{
    [Fact]
    public async Task CanConnectAsync_ReturnsTrue()
    {
        using var context = new SqliteTestContext();

        var result = await context.Repository.CanConnectAsync();

        result.Should().BeTrue();
    }

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public SystemHealthRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new SystemHealthRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }
    }
}
