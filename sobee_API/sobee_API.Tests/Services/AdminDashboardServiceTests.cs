using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Services;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class AdminDashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsSummary()
    {
        var repo = new FakeAdminDashboardRepository
        {
            TotalOrders = 2,
            TotalRevenue = 20m,
            TotalDiscounts = 5m
        };
        var service = new AdminDashboardService(repo);

        var result = await service.GetSummaryAsync();

        result.Success.Should().BeTrue();
        result.Value!.AverageOrderValue.Should().Be(10m);
    }

    [Fact]
    public async Task GetOrdersPerDayAsync_InvalidDays_ReturnsValidationError()
    {
        var service = new AdminDashboardService(new FakeAdminDashboardRepository());

        var result = await service.GetOrdersPerDayAsync(0);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetOrdersPerDayAsync_ReturnsRows()
    {
        var repo = new FakeAdminDashboardRepository();
        repo.OrdersPerDay.Add(new AdminOrderDayRecord(DateTime.UtcNow.Date, 2, 10m));
        var service = new AdminDashboardService(repo);

        var result = await service.GetOrdersPerDayAsync(30);

        result.Success.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLowStockAsync_InvalidThreshold_ReturnsValidationError()
    {
        var service = new AdminDashboardService(new FakeAdminDashboardRepository());

        var result = await service.GetLowStockAsync(-1);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetTopProductsAsync_InvalidLimit_ReturnsValidationError()
    {
        var service = new AdminDashboardService(new FakeAdminDashboardRepository());

        var result = await service.GetTopProductsAsync(0);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetTopProductsAsync_ReturnsProducts()
    {
        var repo = new FakeAdminDashboardRepository();
        repo.TopProducts.Add(new AdminTopProductRecord(1, "Tea", 5, 25m));
        var service = new AdminDashboardService(repo);

        var result = await service.GetTopProductsAsync(5);

        result.Success.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.ProductId == 1);
    }

    private sealed class FakeAdminDashboardRepository : IAdminDashboardRepository
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public List<AdminOrderDayRecord> OrdersPerDay { get; } = new();
        public List<AdminLowStockRecord> LowStock { get; } = new();
        public List<AdminTopProductRecord> TopProducts { get; } = new();

        public Task<int> GetTotalOrdersAsync() => Task.FromResult(TotalOrders);

        public Task<decimal> GetTotalRevenueAsync() => Task.FromResult(TotalRevenue);

        public Task<decimal> GetTotalDiscountsAsync() => Task.FromResult(TotalDiscounts);

        public Task<IReadOnlyList<AdminOrderDayRecord>> GetOrdersPerDayAsync(DateTime fromDate)
            => Task.FromResult((IReadOnlyList<AdminOrderDayRecord>)OrdersPerDay);

        public Task<IReadOnlyList<AdminLowStockRecord>> GetLowStockAsync(int threshold)
            => Task.FromResult((IReadOnlyList<AdminLowStockRecord>)LowStock);

        public Task<IReadOnlyList<AdminTopProductRecord>> GetTopProductsAsync(int limit)
            => Task.FromResult((IReadOnlyList<AdminTopProductRecord>)TopProducts);
    }
}
