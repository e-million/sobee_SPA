using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Orders;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class AdminAnalyticsRepositoryTests
{
    [Fact]
    public async Task GetRevenueByDayAsync_ReturnsDailyTotals()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetRevenueByDayAsync(seed.Day1.AddDays(-1), seed.Day2.AddDays(1));

        results.Should().HaveCount(2);
        results.Sum(r => r.Revenue).Should().Be(30m);
    }

    [Fact]
    public async Task GetRevenueRawAsync_ReturnsRawRecords()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetRevenueRawAsync(seed.Day1.AddDays(-1), seed.Day2.AddDays(1));

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrderStatusesAsync_ReturnsStatuses()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var statuses = await context.Repository.GetOrderStatusesAsync();

        statuses.Should().Contain(new[] { "Paid", "Shipped" });
    }

    [Fact]
    public async Task GetReviewRatingsAsync_ReturnsRatings()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var ratings = await context.Repository.GetReviewRatingsAsync();

        ratings.Should().Contain(new[] { 5, 2 });
    }

    [Fact]
    public async Task GetRecentReviewsAsync_ReturnsRepliesFlag()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var results = await context.Repository.GetRecentReviewsAsync(1);

        results.Should().ContainSingle();
        results[0].HasReplies.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorstProductsAsync_ReturnsLowestUnits()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetWorstProductsAsync(1);

        results.Should().ContainSingle(r => r.ProductId == seed.Product2.IntProductId);
    }

    [Fact]
    public async Task GetCategoryProductCountsAsync_ReturnsCounts()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var results = await context.Repository.GetCategoryProductCountsAsync();

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategorySalesAsync_ReturnsAggregates()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetCategorySalesAsync(seed.Day1.AddDays(-1), seed.Day2.AddDays(1));

        results.Should().HaveCount(2);
        results.Sum(r => r.Revenue).Should().Be(30m);
    }

    [Fact]
    public async Task GetInventorySummaryAsync_ReturnsSummary()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var summary = await context.Repository.GetInventorySummaryAsync(lowStockThreshold: 3);

        summary.TotalProducts.Should().Be(2);
        summary.InStockCount.Should().Be(1);
        summary.OutOfStockCount.Should().Be(1);
        summary.TotalStockValue.Should().Be(10m);
    }

    [Fact]
    public async Task GetFulfillmentOrdersAsync_ReturnsRecords()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetFulfillmentOrdersAsync(seed.Day1.AddDays(-1), seed.Day2.AddDays(1));

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFirstOrdersByUserAsync_ReturnsFirstOrder()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetFirstOrdersByUserAsync();

        results.Should().Contain(r => r.UserId == "user-1" && r.FirstOrder == seed.Day1);
    }

    [Fact]
    public async Task GetUserRevenueInRangeAsync_ReturnsTotals()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetUserRevenueInRangeAsync(seed.Day1.AddDays(-1), seed.Day2.AddDays(1));

        results.Should().Contain(r => r.UserId == "user-1" && r.Total == 10m);
        results.Should().Contain(r => r.UserId == "user-2" && r.Total == 20m);
    }

    [Fact]
    public async Task GetTopCustomersAsync_ReturnsTopCustomer()
    {
        using var context = new SqliteTestContext();
        SeedAnalyticsData(context);

        var results = await context.Repository.GetTopCustomersAsync(1, null, null);

        results.Should().ContainSingle(r => r.UserId == "user-2");
    }

    [Fact]
    public async Task GetMostWishlistedAsync_ReturnsCounts()
    {
        using var context = new SqliteTestContext();
        var seed = SeedAnalyticsData(context);

        var results = await context.Repository.GetMostWishlistedAsync(5);

        results.Should().ContainSingle(r => r.ProductId == seed.Product1.IntProductId && r.WishlistCount == 2);
    }

    private static SeedState SeedAnalyticsData(SqliteTestContext context)
    {
        var day1 = new DateTime(2026, 1, 10);
        var day2 = new DateTime(2026, 1, 11);

        var category1 = context.AddCategory(CreateCategory("Tea"));
        var category2 = context.AddCategory(CreateCategory("Coffee"));

        var product1 = context.AddProduct(CreateProduct(1, "Tea", 5m, 2m, 5, category1));
        var product2 = context.AddProduct(CreateProduct(2, "Coffee", 10m, 4m, 0, category2));

        var order1 = context.AddOrder(CreateOrder("user-1", "Paid", day1, 10m));
        var order2 = context.AddOrder(CreateOrder("user-2", "Shipped", day2, 20m));

        context.AddOrderItem(order1, product1, 2, 5m);
        context.AddOrderItem(order2, product2, 1, 20m);

        var review1 = context.AddReview(CreateReview(product1.IntProductId, 5, day2, "user-1"));
        context.AddReview(CreateReview(product2.IntProductId, 2, day1, "user-2"));
        context.AddReply(CreateReply(review1.IntReviewId, "user-1"));

        context.AddFavorite(CreateFavorite(product1.IntProductId, "user-1"));
        context.AddFavorite(CreateFavorite(product1.IntProductId, "user-2"));

        return new SeedState(day1, day2, product1, product2);
    }

    private static TdrinkCategory CreateCategory(string name)
        => new()
        {
            StrName = name,
            StrDescription = name
        };

    private static Tproduct CreateProduct(
        int id,
        string name,
        decimal price,
        decimal cost,
        int stock,
        TdrinkCategory category)
        => new()
        {
            IntProductId = id,
            StrName = name,
            strDescription = name,
            DecPrice = price,
            DecCost = cost,
            IntStockAmount = stock,
            IntDrinkCategoryId = category.IntDrinkCategoryId,
            IntDrinkCategory = category
        };

    private static Torder CreateOrder(string userId, string status, DateTime date, decimal total)
        => new()
        {
            UserId = userId,
            StrOrderStatus = status,
            DtmOrderDate = date,
            DecTotalAmount = total,
            DtmShippedDate = date.AddDays(1),
            DtmDeliveredDate = date.AddDays(2)
        };

    private static Treview CreateReview(int productId, int rating, DateTime date, string userId)
        => new()
        {
            IntProductId = productId,
            IntRating = rating,
            StrReviewText = "Review",
            DtmReviewDate = date,
            UserId = userId
        };

    private static TReviewReplies CreateReply(int reviewId, string userId)
        => new()
        {
            IntReviewId = reviewId,
            UserId = userId,
            content = "Reply",
            created_at = DateTime.UtcNow
        };

    private static Tfavorite CreateFavorite(int productId, string userId)
        => new()
        {
            IntProductId = productId,
            UserId = userId,
            DtmDateAdded = DateTime.UtcNow
        };

    private sealed record SeedState(DateTime Day1, DateTime Day2, Tproduct Product1, Tproduct Product2);

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public AdminAnalyticsRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new AdminAnalyticsRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public TdrinkCategory AddCategory(TdrinkCategory category)
        {
            DbContext.TdrinkCategories.Add(category);
            DbContext.SaveChanges();
            return category;
        }

        public Tproduct AddProduct(Tproduct product)
        {
            DbContext.Tproducts.Add(product);
            DbContext.SaveChanges();
            return product;
        }

        public Torder AddOrder(Torder order)
        {
            DbContext.Torders.Add(order);
            DbContext.SaveChanges();
            return order;
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

        public Treview AddReview(Treview review)
        {
            DbContext.Treviews.Add(review);
            DbContext.SaveChanges();
            return review;
        }

        public TReviewReplies AddReply(TReviewReplies reply)
        {
            DbContext.TReviewReplies.Add(reply);
            DbContext.SaveChanges();
            return reply;
        }

        public Tfavorite AddFavorite(Tfavorite favorite)
        {
            DbContext.Tfavorites.Add(favorite);
            DbContext.SaveChanges();
            return favorite;
        }
    }
}
