using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class AdminPromoRepositoryTests
{
    [Fact]
    public async Task GetPromosAsync_ExcludesExpired()
    {
        using var context = new SqliteTestContext();
        context.AddPromo(CreatePromo(1, "ACTIVE", DateTime.UtcNow.AddDays(1)));
        context.AddPromo(CreatePromo(2, "EXPIRED", DateTime.UtcNow.AddDays(-1)));

        var (items, total) = await context.Repository.GetPromosAsync(null, includeExpired: false, page: 1, pageSize: 10);

        total.Should().Be(1);
        items.Should().ContainSingle(p => p.StrPromoCode == "ACTIVE");
    }

    [Fact]
    public async Task GetUsageCountsAsync_ReturnsCounts()
    {
        using var context = new SqliteTestContext();
        context.AddOrder(CreateOrder("SAVE10"));
        context.AddOrder(CreateOrder("SAVE10"));
        context.AddOrder(CreateOrder("SAVE20"));

        var result = await context.Repository.GetUsageCountsAsync(new[] { "SAVE10", "SAVE20" });

        result.Should().ContainSingle(x => x.Code == "SAVE10" && x.Count == 2);
        result.Should().ContainSingle(x => x.Code == "SAVE20" && x.Count == 1);
    }

    [Fact]
    public async Task ExistsByCodeAsync_ReturnsTrue()
    {
        using var context = new SqliteTestContext();
        context.AddPromo(CreatePromo(1, "SAVE10", DateTime.UtcNow.AddDays(1)));

        var exists = await context.Repository.ExistsByCodeAsync("SAVE10");

        exists.Should().BeTrue();
    }

    private static Tpromotion CreatePromo(int id, string code, DateTime expiresAt)
        => new()
        {
            IntPromotionId = id,
            StrPromoCode = code,
            StrDiscountPercentage = "10",
            DecDiscountPercentage = 10m,
            DtmExpirationDate = expiresAt
        };

    private static Torder CreateOrder(string promoCode)
        => new()
        {
            StrPromoCode = promoCode,
            DtmOrderDate = DateTime.UtcNow,
            DecTotalAmount = 10m
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public AdminPromoRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new AdminPromoRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public Tpromotion AddPromo(Tpromotion promo)
        {
            DbContext.Tpromotions.Add(promo);
            DbContext.SaveChanges();
            return promo;
        }

        public Torder AddOrder(Torder order)
        {
            DbContext.Torders.Add(order);
            DbContext.SaveChanges();
            return order;
        }
    }
}
