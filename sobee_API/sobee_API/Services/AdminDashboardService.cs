using sobee_API.Domain;
using sobee_API.DTOs.Admin;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IAdminDashboardRepository _repository;

    public AdminDashboardService(IAdminDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<AdminSummaryResponse>> GetSummaryAsync()
    {
        var totalOrders = await _repository.GetTotalOrdersAsync();
        var totalRevenue = await _repository.GetTotalRevenueAsync();
        var totalDiscounts = await _repository.GetTotalDiscountsAsync();

        var averageOrderValue = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

        return ServiceResult<AdminSummaryResponse>.Ok(new AdminSummaryResponse
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            TotalDiscounts = totalDiscounts,
            AverageOrderValue = averageOrderValue
        });
    }

    public async Task<ServiceResult<IReadOnlyList<OrdersPerDayResponse>>> GetOrdersPerDayAsync(int days)
    {
        if (days <= 0 || days > 365)
        {
            return Validation<IReadOnlyList<OrdersPerDayResponse>>("Days must be between 1 and 365.", null);
        }

        var fromDate = DateTime.UtcNow.Date.AddDays(-days);
        var data = await _repository.GetOrdersPerDayAsync(fromDate);

        var response = data.Select(row => new OrdersPerDayResponse
        {
            Date = row.Date,
            Count = row.Count,
            Revenue = row.Revenue
        }).ToList();

        return ServiceResult<IReadOnlyList<OrdersPerDayResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyList<LowStockProductResponse>>> GetLowStockAsync(int threshold)
    {
        if (threshold < 0)
        {
            return Validation<IReadOnlyList<LowStockProductResponse>>("Threshold cannot be negative.", null);
        }

        var products = await _repository.GetLowStockAsync(threshold);
        var response = products.Select(product => new LowStockProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            StockAmount = product.StockAmount
        }).ToList();

        return ServiceResult<IReadOnlyList<LowStockProductResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyList<TopProductResponse>>> GetTopProductsAsync(int limit)
    {
        if (limit <= 0 || limit > 50)
        {
            return Validation<IReadOnlyList<TopProductResponse>>("Limit must be between 1 and 50.", null);
        }

        var products = await _repository.GetTopProductsAsync(limit);
        var response = products.Select(product => new TopProductResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            QuantitySold = product.QuantitySold,
            Revenue = product.Revenue
        }).ToList();

        return ServiceResult<IReadOnlyList<TopProductResponse>>.Ok(response);
    }

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);
}
