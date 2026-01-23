using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Services;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class AdminAnalyticsServiceTests
{
    [Fact]
    public async Task GetRevenueByPeriodAsync_InvalidGranularity_ReturnsValidationError()
    {
        var service = CreateService();

        var result = await service.GetRevenueByPeriodAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "quarter");

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetRevenueByPeriodAsync_Day_ReturnsRows()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.RevenueByDay.Add(new AdminRevenueRow(DateTime.UtcNow.Date, 10m, 2, 5m));
        var service = CreateService(repo);

        var result = await service.GetRevenueByPeriodAsync(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, "day");

        result.Success.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.Revenue == 10m && r.OrderCount == 2);
    }

    [Fact]
    public async Task GetRevenueByPeriodAsync_Week_GroupsRaw()
    {
        var repo = new FakeAdminAnalyticsRepository();
        var baseDate = new DateTime(2026, 1, 6);
        repo.RevenueRaw.Add(new AdminRevenueRawRecord(baseDate, 10m));
        repo.RevenueRaw.Add(new AdminRevenueRawRecord(baseDate.AddDays(1), 15m));
        var service = CreateService(repo);

        var result = await service.GetRevenueByPeriodAsync(baseDate.AddDays(-1), baseDate.AddDays(2), "week");

        result.Success.Should().BeTrue();
        result.Value.Should().ContainSingle(r => r.Revenue == 25m && r.OrderCount == 2);
    }

    [Fact]
    public async Task GetOrderStatusBreakdownAsync_MapsStatuses()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.OrderStatuses.AddRange(new[] { "Paid", " paid ", "Unknown", null });
        var service = CreateService(repo);

        var result = await service.GetOrderStatusBreakdownAsync();

        result.Success.Should().BeTrue();
        result.Value!.Paid.Should().Be(2);
        result.Value.Other.Should().Be(1);
        result.Value.Pending.Should().Be(1);
    }

    [Fact]
    public async Task GetRatingDistributionAsync_MapsDistribution()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.ReviewRatings.AddRange(new[] { 1, 5, 5 });
        var service = CreateService(repo);

        var result = await service.GetRatingDistributionAsync();

        result.Success.Should().BeTrue();
        result.Value!.Distribution.FiveStar.Should().Be(2);
        result.Value.AverageRating.Should().BeApproximately(3.67m, 0.01m);
    }

    [Fact]
    public async Task GetRecentReviewsAsync_InvalidLimit_ReturnsValidationError()
    {
        var service = CreateService();

        var result = await service.GetRecentReviewsAsync(0);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetCategoryPerformanceAsync_InvalidDateRange_ReturnsValidationError()
    {
        var service = CreateService();
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);

        var result = await service.GetCategoryPerformanceAsync(start, end);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetInventorySummaryAsync_InvalidThreshold_ReturnsValidationError()
    {
        var service = CreateService();

        var result = await service.GetInventorySummaryAsync(-1);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetFulfillmentMetricsAsync_ReturnsMetrics()
    {
        var repo = new FakeAdminAnalyticsRepository();
        var current = new List<AdminFulfillmentRecord>
        {
            new(new DateTime(2026, 1, 10), new DateTime(2026, 1, 11), new DateTime(2026, 1, 12)),
            new(new DateTime(2026, 1, 11), new DateTime(2026, 1, 11, 12, 0, 0), new DateTime(2026, 1, 12))
        };
        var previous = new List<AdminFulfillmentRecord>
        {
            new(new DateTime(2026, 1, 8), new DateTime(2026, 1, 8, 12, 0, 0), null)
        };
        repo.FulfillmentQueue.Enqueue(current);
        repo.FulfillmentQueue.Enqueue(previous);
        var service = CreateService(repo);

        var result = await service.GetFulfillmentMetricsAsync(new DateTime(2026, 1, 10), new DateTime(2026, 1, 12));

        result.Success.Should().BeTrue();
        result.Value!.AvgHoursToShip.Should().Be(18m);
        result.Value.Trend.Should().Be(50m);
    }

    [Fact]
    public async Task GetCustomerBreakdownAsync_ReturnsBreakdown()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.FirstOrdersByUser.Add(new AdminUserFirstOrderRecord("user-1", new DateTime(2026, 1, 1)));
        repo.FirstOrdersByUser.Add(new AdminUserFirstOrderRecord("user-2", new DateTime(2025, 12, 1)));
        repo.UserRevenueInRange.Add(new AdminUserRevenueRecord("user-1", 10m));
        repo.UserRevenueInRange.Add(new AdminUserRevenueRecord("user-2", 20m));
        var service = CreateService(repo);

        var result = await service.GetCustomerBreakdownAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        result.Success.Should().BeTrue();
        result.Value!.NewCustomers.Should().Be(1);
        result.Value.ReturningCustomers.Should().Be(1);
        result.Value.NewCustomerRevenue.Should().Be(10m);
        result.Value.ReturningCustomerRevenue.Should().Be(20m);
    }

    [Fact]
    public async Task GetCustomerGrowthAsync_ReturnsGrowth()
    {
        var repo = new FakeAdminAnalyticsRepository();
        var userRepo = new FakeAdminUserRepository
        {
            BaselineCount = 2,
            Registrations = new List<DateTime>
            {
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 2)
            }
        };
        var service = CreateService(repo, userRepo);

        var result = await service.GetCustomerGrowthAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), "day");

        result.Success.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Last().CumulativeTotal.Should().Be(5);
    }

    [Fact]
    public async Task GetTopCustomersAsync_ReturnsEnriched()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.TopCustomers.Add(new AdminTopCustomerRecord("user-1", 50m, 2, DateTime.UtcNow));
        var userRepo = new FakeAdminUserRepository();
        userRepo.Profiles.Add(new AdminUserProfileRecord("user-1", "user1@demo.com", "A", "One"));
        var service = CreateService(repo, userRepo);

        var result = await service.GetTopCustomersAsync(5, null, null);

        result.Success.Should().BeTrue();
        result.Value.Should().ContainSingle(c => c.UserId == "user-1" && c.Email == "user1@demo.com");
    }

    [Fact]
    public async Task GetMostWishlistedAsync_InvalidLimit_ReturnsValidationError()
    {
        var service = CreateService();

        var result = await service.GetMostWishlistedAsync(0);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetMostWishlistedAsync_ReturnsItems()
    {
        var repo = new FakeAdminAnalyticsRepository();
        repo.Wishlisted.Add(new AdminWishlistedRecord(1, "Tea", 3));
        var service = CreateService(repo);

        var result = await service.GetMostWishlistedAsync(5);

        result.Success.Should().BeTrue();
        result.Value.Should().ContainSingle(i => i.ProductId == 1 && i.WishlistCount == 3);
    }

    private static AdminAnalyticsService CreateService(
        FakeAdminAnalyticsRepository? analyticsRepo = null,
        FakeAdminUserRepository? userRepo = null)
    {
        return new AdminAnalyticsService(
            analyticsRepo ?? new FakeAdminAnalyticsRepository(),
            userRepo ?? new FakeAdminUserRepository());
    }

    private sealed class FakeAdminAnalyticsRepository : IAdminAnalyticsRepository
    {
        public List<AdminRevenueRow> RevenueByDay { get; } = new();
        public List<AdminRevenueRawRecord> RevenueRaw { get; } = new();
        public List<string?> OrderStatuses { get; } = new();
        public List<int> ReviewRatings { get; } = new();
        public List<AdminRecentReviewRecord> RecentReviews { get; } = new();
        public List<AdminWorstProductRecord> WorstProducts { get; } = new();
        public List<AdminCategoryProductCountRecord> CategoryProductCounts { get; } = new();
        public List<AdminCategorySalesRecord> CategorySales { get; } = new();
        public AdminInventorySummaryRecord InventorySummary { get; set; } = new(0, 0, 0, 0, 0m);
        public Queue<IReadOnlyList<AdminFulfillmentRecord>> FulfillmentQueue { get; } = new();
        public List<AdminUserFirstOrderRecord> FirstOrdersByUser { get; } = new();
        public List<AdminUserRevenueRecord> UserRevenueInRange { get; } = new();
        public List<AdminTopCustomerRecord> TopCustomers { get; } = new();
        public List<AdminWishlistedRecord> Wishlisted { get; } = new();

        public Task<IReadOnlyList<AdminRevenueRow>> GetRevenueByDayAsync(DateTime start, DateTime end)
            => Task.FromResult((IReadOnlyList<AdminRevenueRow>)RevenueByDay);

        public Task<IReadOnlyList<AdminRevenueRawRecord>> GetRevenueRawAsync(DateTime start, DateTime end)
            => Task.FromResult((IReadOnlyList<AdminRevenueRawRecord>)RevenueRaw);

        public Task<IReadOnlyList<string?>> GetOrderStatusesAsync()
            => Task.FromResult((IReadOnlyList<string?>)OrderStatuses);

        public Task<IReadOnlyList<int>> GetReviewRatingsAsync()
            => Task.FromResult((IReadOnlyList<int>)ReviewRatings);

        public Task<IReadOnlyList<AdminRecentReviewRecord>> GetRecentReviewsAsync(int limit)
            => Task.FromResult((IReadOnlyList<AdminRecentReviewRecord>)RecentReviews.Take(limit).ToList());

        public Task<IReadOnlyList<AdminWorstProductRecord>> GetWorstProductsAsync(int limit)
            => Task.FromResult((IReadOnlyList<AdminWorstProductRecord>)WorstProducts.Take(limit).ToList());

        public Task<IReadOnlyList<AdminCategoryProductCountRecord>> GetCategoryProductCountsAsync()
            => Task.FromResult((IReadOnlyList<AdminCategoryProductCountRecord>)CategoryProductCounts);

        public Task<IReadOnlyList<AdminCategorySalesRecord>> GetCategorySalesAsync(DateTime start, DateTime end)
            => Task.FromResult((IReadOnlyList<AdminCategorySalesRecord>)CategorySales);

        public Task<AdminInventorySummaryRecord> GetInventorySummaryAsync(int lowStockThreshold)
            => Task.FromResult(InventorySummary);

        public Task<IReadOnlyList<AdminFulfillmentRecord>> GetFulfillmentOrdersAsync(DateTime start, DateTime end)
        {
            if (FulfillmentQueue.Count > 0)
            {
                return Task.FromResult(FulfillmentQueue.Dequeue());
            }

            return Task.FromResult<IReadOnlyList<AdminFulfillmentRecord>>(Array.Empty<AdminFulfillmentRecord>());
        }

        public Task<IReadOnlyList<AdminUserFirstOrderRecord>> GetFirstOrdersByUserAsync()
            => Task.FromResult((IReadOnlyList<AdminUserFirstOrderRecord>)FirstOrdersByUser);

        public Task<IReadOnlyList<AdminUserRevenueRecord>> GetUserRevenueInRangeAsync(DateTime start, DateTime end)
            => Task.FromResult((IReadOnlyList<AdminUserRevenueRecord>)UserRevenueInRange);

        public Task<IReadOnlyList<AdminTopCustomerRecord>> GetTopCustomersAsync(int limit, DateTime? start, DateTime? end)
            => Task.FromResult((IReadOnlyList<AdminTopCustomerRecord>)TopCustomers.Take(limit).ToList());

        public Task<IReadOnlyList<AdminWishlistedRecord>> GetMostWishlistedAsync(int limit)
            => Task.FromResult((IReadOnlyList<AdminWishlistedRecord>)Wishlisted.Take(limit).ToList());
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        public int BaselineCount { get; set; }
        public List<DateTime> Registrations { get; set; } = new();
        public List<AdminUserProfileRecord> Profiles { get; } = new();

        public Task<(IReadOnlyList<AdminUserRecord> Users, int TotalCount)> GetUsersAsync(string? search, int page, int pageSize)
            => Task.FromResult(((IReadOnlyList<AdminUserRecord>)Array.Empty<AdminUserRecord>(), 0));

        public Task<IReadOnlyList<AdminUserRoleRecord>> GetUserRolesAsync(IReadOnlyList<string> userIds)
            => Task.FromResult((IReadOnlyList<AdminUserRoleRecord>)Array.Empty<AdminUserRoleRecord>());

        public Task<IReadOnlyList<AdminUserProfileRecord>> GetUsersByIdsAsync(IReadOnlyList<string> userIds)
        {
            IReadOnlyList<AdminUserProfileRecord> profiles = Profiles.Where(p => userIds.Contains(p.Id)).ToList();
            return Task.FromResult(profiles);
        }

        public Task<int> GetUserCountBeforeAsync(DateTime start)
            => Task.FromResult(BaselineCount);

        public Task<IReadOnlyList<DateTime>> GetUserRegistrationsAsync(DateTime start, DateTime end)
            => Task.FromResult((IReadOnlyList<DateTime>)Registrations);
    }
}
