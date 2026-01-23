using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using sobee_API.Constants;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class OrderRepositoryTests
{
    [Fact]
    public async Task FindByIdAsync_Exists_ReturnsOrder()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));

        var result = await context.Repository.FindByIdAsync(order.IntOrderId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task FindByIdWithItemsAsync_ReturnsItems()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));
        var product = context.AddProduct(CreateProduct(1, 5m));
        context.AddOrderItem(order, product, 2, 5m);

        var result = await context.Repository.FindByIdWithItemsAsync(order.IntOrderId);

        result.Should().NotBeNull();
        result!.TorderItems.Should().HaveCount(1);
        result.TorderItems.First().IntQuantity.Should().Be(2);
    }

    [Fact]
    public async Task FindForOwnerAsync_User_ReturnsOrder()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));

        var result = await context.Repository.FindForOwnerAsync(order.IntOrderId, "user-1", null);

        result.Should().NotBeNull();
        result!.IntOrderId.Should().Be(order.IntOrderId);
    }

    [Fact]
    public async Task FindForOwnerAsync_WrongOwner_ReturnsNull()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));

        var result = await context.Repository.FindForOwnerAsync(order.IntOrderId, "user-2", null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserOrdersAsync_ReturnsPagedResults()
    {
        using var context = new SqliteTestContext();
        context.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow.AddDays(-2)));
        context.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow.AddDays(-1)));
        context.AddOrder(CreateOrder(userId: "user-1", orderDate: DateTime.UtcNow));

        var result = await context.Repository.GetUserOrdersAsync("user-1", page: 1, pageSize: 2);

        result.Should().HaveCount(2);
        result[0].DtmOrderDate.Should().BeAfter((DateTime)result[1].DtmOrderDate);
    }

    [Fact]
    public async Task CountUserOrdersAsync_ReturnsTotal()
    {
        using var context = new SqliteTestContext();
        context.AddOrder(CreateOrder(userId: "user-1"));
        context.AddOrder(CreateOrder(userId: "user-1"));
        context.AddOrder(CreateOrder(userId: "user-2"));

        var result = await context.Repository.CountUserOrdersAsync("user-1");

        result.Should().Be(2);
    }

    [Fact]
    public async Task AddItemsAsync_AddsOrderItems()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));
        var product = context.AddProduct(CreateProduct(1, 5m));

        await context.Repository.AddItemsAsync(new[]
        {
            new TorderItem
            {
                IntOrderId = order.IntOrderId,
                IntProductId = product.IntProductId,
                IntQuantity = 2,
                MonPricePerUnit = 5m
            }
        });
        await context.Repository.SaveChangesAsync();

        var items = await context.DbContext.TorderItems.CountAsync();
        items.Should().Be(1);
    }

    [Fact]
    public async Task BeginTransactionAsync_AllowsRollback()
    {
        using var context = new SqliteTestContext();
        var order = context.AddOrder(CreateOrder(userId: "user-1"));

        using var tx = await context.Repository.BeginTransactionAsync();
        order.StrOrderStatus = OrderStatuses.Cancelled;
        await context.Repository.SaveChangesAsync();
        await tx.RollbackAsync();

        var reloaded = await context.Repository.FindByIdAsync(order.IntOrderId, track: false);
        reloaded!.StrOrderStatus.Should().Be(OrderStatuses.Pending);
    }

    private static Torder CreateOrder(string? userId = null, string? sessionId = null, DateTime? orderDate = null)
        => new()
        {
            UserId = userId,
            SessionId = sessionId,
            StrOrderStatus = OrderStatuses.Pending,
            DtmOrderDate = orderDate ?? DateTime.UtcNow,
            DecTotalAmount = 10m
        };

    private static Tproduct CreateProduct(int id, decimal price)
        => new()
        {
            IntProductId = id,
            StrName = $"Product-{id}",
            strDescription = $"Product-{id}",
            DecPrice = price,
            IntStockAmount = 10
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public OrderRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new OrderRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public Torder AddOrder(Torder order)
        {
            DbContext.Torders.Add(order);
            DbContext.SaveChanges();
            return order;
        }

        public Tproduct AddProduct(Tproduct product)
        {
            DbContext.Tproducts.Add(product);
            DbContext.SaveChanges();
            return product;
        }

        public TorderItem AddOrderItem(Torder order, Tproduct product, int quantity, decimal price)
        {
            var item = new TorderItem
            {
                IntOrderId = order.IntOrderId,
                IntProductId = product.IntProductId,
                IntQuantity = quantity,
                MonPricePerUnit = price
            };

            DbContext.TorderItems.Add(item);
            DbContext.SaveChanges();
            return item;
        }
    }
}
