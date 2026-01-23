using sobee_API.Domain;
using sobee_API.DTOs.Admin;

namespace sobee_API.Services.Interfaces;

public interface IAdminAnalyticsService
{
    Task<ServiceResult<IReadOnlyList<RevenueByPeriodResponse>>> GetRevenueByPeriodAsync(DateTime? startDate, DateTime? endDate, string granularity);
    Task<ServiceResult<OrderStatusBreakdownResponse>> GetOrderStatusBreakdownAsync();
    Task<ServiceResult<RatingDistributionResponse>> GetRatingDistributionAsync();
    Task<ServiceResult<IReadOnlyList<RecentReviewResponse>>> GetRecentReviewsAsync(int limit);
    Task<ServiceResult<IReadOnlyList<WorstProductResponse>>> GetWorstProductsAsync(int limit);
    Task<ServiceResult<IReadOnlyList<CategoryPerformanceResponse>>> GetCategoryPerformanceAsync(DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<InventorySummaryResponse>> GetInventorySummaryAsync(int lowStockThreshold);
    Task<ServiceResult<FulfillmentMetricsResponse>> GetFulfillmentMetricsAsync(DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<CustomerBreakdownResponse>> GetCustomerBreakdownAsync(DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<IReadOnlyList<CustomerGrowthResponse>>> GetCustomerGrowthAsync(DateTime? startDate, DateTime? endDate, string granularity);
    Task<ServiceResult<IReadOnlyList<TopCustomerResponse>>> GetTopCustomersAsync(int limit, DateTime? startDate, DateTime? endDate);
    Task<ServiceResult<IReadOnlyList<WishlistedProductResponse>>> GetMostWishlistedAsync(int limit);
}
