using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class CartRepositoryTests
{
    [Fact]
    public async Task FindByUserIdAsync_Exists_ReturnsCart()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, 4.50m, 10));
        var cart = context.AddCart(CreateCart(userId: "user-1"));
        context.AddCartItem(cart, product, 2);

        var result = await context.Repository.FindByUserIdAsync("user-1");

        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
        result.TcartItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindByUserIdAsync_NotExists_ReturnsNull()
    {
        using var context = new SqliteTestContext();

        var result = await context.Repository.FindByUserIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindBySessionIdAsync_Exists_ReturnsCart()
    {
        using var context = new SqliteTestContext();
        var cart = context.AddCart(CreateCart(sessionId: "session-1"));

        var result = await context.Repository.FindBySessionIdAsync("session-1");

        result.Should().NotBeNull();
        result!.SessionId.Should().Be("session-1");
        result.UserId.Should().BeNull();
        result.IntShoppingCartId.Should().Be(cart.IntShoppingCartId);
    }

    [Fact]
    public async Task CreateAsync_CreatesCart()
    {
        using var context = new SqliteTestContext();
        var cart = CreateCart(sessionId: "session-1");

        await context.Repository.CreateAsync(cart);
        await context.Repository.SaveChangesAsync();

        var result = await context.Repository.FindBySessionIdAsync("session-1");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddCartItemAsync_AddsItem()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.AddCart(CreateCart(sessionId: "session-1"));

        var item = new TcartItem
        {
            IntShoppingCartId = cart.IntShoppingCartId,
            IntProductId = product.IntProductId,
            IntQuantity = 2,
            DtmDateAdded = DateTime.UtcNow
        };

        await context.Repository.AddCartItemAsync(item);
        await context.Repository.SaveChangesAsync();

        var loaded = await context.Repository.LoadCartWithItemsAsync(cart.IntShoppingCartId);
        loaded.TcartItems.Should().HaveCount(1);
        loaded.TcartItems[0].IntQuantity.Should().Be(2);
    }

    [Fact]
    public async Task UpdateCartItemAsync_UpdatesQuantity()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.AddCartItem(cart, product, 1);

        item.IntQuantity = 3;
        await context.Repository.UpdateCartItemAsync(item);
        await context.Repository.SaveChangesAsync();

        var updated = await context.DbContext.TcartItems.FirstAsync();
        updated.IntQuantity.Should().Be(3);
    }

    [Fact]
    public async Task RemoveCartItemAsync_RemovesItem()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, 5m, 10));
        var cart = context.AddCart(CreateCart(sessionId: "session-1"));
        var item = context.AddCartItem(cart, product, 1);

        await context.Repository.RemoveCartItemAsync(item);
        await context.Repository.SaveChangesAsync();

        var remaining = await context.DbContext.TcartItems.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task ClearCartItemsAsync_RemovesAllItems()
    {
        using var context = new SqliteTestContext();
        var product1 = context.AddProduct(CreateProduct(1, 5m, 10));
        var product2 = context.AddProduct(CreateProduct(2, 6m, 10));
        var cart = context.AddCart(CreateCart(sessionId: "session-1"));
        context.AddCartItem(cart, product1, 1);
        context.AddCartItem(cart, product2, 2);

        await context.Repository.ClearCartItemsAsync(cart.IntShoppingCartId);
        await context.Repository.SaveChangesAsync();

        var remaining = await context.DbContext.TcartItems.CountAsync();
        remaining.Should().Be(0);
    }

    private static Tproduct CreateProduct(int id, decimal price, int stock)
        => new()
        {
            IntProductId = id,
            StrName = $"Product-{id}",
            strDescription = $"Product-{id}",
            DecPrice = price,
            IntStockAmount = stock
        };

    private static TshoppingCart CreateCart(string? userId = null, string? sessionId = null)
        => new()
        {
            UserId = userId,
            SessionId = sessionId,
            DtmDateCreated = DateTime.UtcNow,
            DtmDateLastUpdated = DateTime.UtcNow
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public CartRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new CartRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public Tproduct AddProduct(Tproduct product)
        {
            DbContext.Tproducts.Add(product);
            DbContext.SaveChanges();
            return product;
        }

        public TshoppingCart AddCart(TshoppingCart cart)
        {
            DbContext.TshoppingCarts.Add(cart);
            DbContext.SaveChanges();
            return cart;
        }

        public TcartItem AddCartItem(TshoppingCart cart, Tproduct product, int quantity)
        {
            var item = new TcartItem
            {
                IntShoppingCartId = cart.IntShoppingCartId,
                IntProductId = product.IntProductId,
                IntQuantity = quantity,
                DtmDateAdded = DateTime.UtcNow
            };

            DbContext.TcartItems.Add(item);
            DbContext.SaveChanges();
            return item;
        }
    }
}
