using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Promotions;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class PromoRepositoryTests
{
    [Fact]
    public async Task FindActiveByCodeAsync_ReturnsPromo()
    {
        using var context = new SqliteTestContext();
        context.AddPromotion(CreatePromo(1, "SAVE10", DateTime.UtcNow.AddDays(1)));

        var promo = await context.Repository.FindActiveByCodeAsync("SAVE10", DateTime.UtcNow);

        promo.Should().NotBeNull();
        promo!.DecDiscountPercentage.Should().Be(10m);
    }

    [Fact]
    public async Task UsageExistsAsync_ReturnsTrue()
    {
        using var context = new SqliteTestContext();
        var cart = context.AddCart(CreateCart());
        context.AddUsage(CreateUsage(cart.IntShoppingCartId, "SAVE10"));

        var exists = await context.Repository.UsageExistsAsync(cart.IntShoppingCartId, "SAVE10");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetActivePromoForCartAsync_ReturnsPromo()
    {
        using var context = new SqliteTestContext();
        var cart = context.AddCart(CreateCart());
        context.AddPromotion(CreatePromo(1, "SAVE10", DateTime.UtcNow.AddDays(1)));
        context.AddUsage(CreateUsage(cart.IntShoppingCartId, "SAVE10"));

        var (code, discount) = await context.Repository.GetActivePromoForCartAsync(cart.IntShoppingCartId, DateTime.UtcNow);

        code.Should().Be("SAVE10");
        discount.Should().Be(10m);
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

    private static TshoppingCart CreateCart()
        => new()
        {
            DtmDateCreated = DateTime.UtcNow,
            DtmDateLastUpdated = DateTime.UtcNow,
            SessionId = "session-1"
        };

    private static TpromoCodeUsageHistory CreateUsage(int cartId, string code)
        => new()
        {
            IntShoppingCartId = cartId,
            PromoCode = code,
            UsedDateTime = DateTime.UtcNow
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public PromoRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new PromoRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public Tpromotion AddPromotion(Tpromotion promo)
        {
            DbContext.Tpromotions.Add(promo);
            DbContext.SaveChanges();
            return promo;
        }

        public TshoppingCart AddCart(TshoppingCart cart)
        {
            DbContext.TshoppingCarts.Add(cart);
            DbContext.SaveChanges();
            return cart;
        }

        public TpromoCodeUsageHistory AddUsage(TpromoCodeUsageHistory usage)
        {
            DbContext.TpromoCodeUsageHistories.Add(usage);
            DbContext.SaveChanges();
            return usage;
        }
    }
}
