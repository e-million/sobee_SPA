using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class AdminDashboardRepositoryTests
{
    [Fact]
    public async Task GetTotals_ReturnsExpectedValues()
    {
        using var context = new SqliteTestContext();
        context.AddOrder(CreateOrder(10m, 1m, DateTime.UtcNow));
        context.AddOrder(CreateOrder(20m, 2m, DateTime.UtcNow));

        var totalOrders = await context.Repository.GetTotalOrdersAsync();
        var totalRevenue = await context.Repository.GetTotalRevenueAsync();
        var totalDiscounts = await context.Repository.GetTotalDiscountsAsync();

        totalOrders.Should().Be(2);
        totalRevenue.Should().Be(30m);
        totalDiscounts.Should().Be(3m);
    }

    [Fact]
    public async Task GetOrdersPerDayAsync_ReturnsDailyCounts()
    {
        using var context = new SqliteTestContext();
        var day1 = new DateTime(2026, 1, 10);
        var day2 = new DateTime(2026, 1, 11);
        context.AddOrder(CreateOrder(10m, 0m, day1));
        context.AddOrder(CreateOrder(15m, 0m, day2));
        context.AddOrder(CreateOrder(5m, 0m, day2));

        var results = await context.Repository.GetOrdersPerDayAsync(day1.AddDays(-1));

        results.Should().HaveCount(2);
        results.Single(r => r.Date == day1).Count.Should().Be(1);
        results.Single(r => r.Date == day2).Count.Should().Be(2);
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsProducts()
    {
        using var context = new SqliteTestContext();
        context.AddProduct(CreateProduct(1, "Low", 1));
        context.AddProduct(CreateProduct(2, "High", 10));

        var results = await context.Repository.GetLowStockAsync(threshold: 2);

        results.Should().ContainSingle(r => r.ProductId == 1);
    }

    [Fact]
    public async Task GetTopProductsAsync_ReturnsAggregates()
    {
        using var context = new SqliteTestContext();
        var product1 = context.AddProduct(CreateProduct(1, "A", 10));
        var product2 = context.AddProduct(CreateProduct(2, "B", 10));
        var order = context.AddOrder(CreateOrder(20m, 0m, DateTime.UtcNow));
        context.AddOrderItem(order, product1, 2, 5m);
        context.AddOrderItem(order, product2, 1, 10m);

        var results = await context.Repository.GetTopProductsAsync(5);

        results.Should().ContainSingle(r => r.ProductId == product1.IntProductId && r.QuantitySold == 2);
    }

    private static Torder CreateOrder(decimal total, decimal discount, DateTime date)
        => new()
        {
            DecTotalAmount = total,
            DecDiscountAmount = discount,
            DtmOrderDate = date
        };

    private static Tproduct CreateProduct(int id, string name, int stock)
        => new()
        {
            IntProductId = id,
            StrName = name,
            strDescription = name,
            DecPrice = 5m,
            IntStockAmount = stock
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public AdminDashboardRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new AdminDashboardRepository(DbContext);
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
