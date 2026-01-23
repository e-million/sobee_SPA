namespace Sobee.Domain.Repositories;

public interface IAdminAnalyticsRepository
{
    Task<IReadOnlyList<AdminRevenueRow>> GetRevenueByDayAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<AdminRevenueRawRecord>> GetRevenueRawAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<string?>> GetOrderStatusesAsync();
    Task<IReadOnlyList<int>> GetReviewRatingsAsync();
    Task<IReadOnlyList<AdminRecentReviewRecord>> GetRecentReviewsAsync(int limit);
    Task<IReadOnlyList<AdminWorstProductRecord>> GetWorstProductsAsync(int limit);
    Task<IReadOnlyList<AdminCategoryProductCountRecord>> GetCategoryProductCountsAsync();
    Task<IReadOnlyList<AdminCategorySalesRecord>> GetCategorySalesAsync(DateTime start, DateTime end);
    Task<AdminInventorySummaryRecord> GetInventorySummaryAsync(int lowStockThreshold);
    Task<IReadOnlyList<AdminFulfillmentRecord>> GetFulfillmentOrdersAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<AdminUserFirstOrderRecord>> GetFirstOrdersByUserAsync();
    Task<IReadOnlyList<AdminUserRevenueRecord>> GetUserRevenueInRangeAsync(DateTime start, DateTime end);
    Task<IReadOnlyList<AdminTopCustomerRecord>> GetTopCustomersAsync(int limit, DateTime? start, DateTime? end);
    Task<IReadOnlyList<AdminWishlistedRecord>> GetMostWishlistedAsync(int limit);
}

public sealed record AdminRevenueRow(DateTime Date, decimal Revenue, int OrderCount, decimal AvgOrderValue);

public sealed record AdminRevenueRawRecord(DateTime Date, decimal Revenue);

public sealed record AdminRecentReviewRecord(
    int ReviewId,
    int ProductId,
    string? ProductName,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    string? UserId,
    bool HasReplies);

public sealed record AdminWorstProductRecord(
    int ProductId,
    string? Name,
    int UnitsSold,
    decimal Revenue);

public sealed record AdminCategoryProductCountRecord(
    int? CategoryId,
    string? CategoryName,
    int ProductCount);

public sealed record AdminCategorySalesRecord(
    int? CategoryId,
    string? CategoryName,
    int UnitsSold,
    decimal Revenue);

public sealed record AdminInventorySummaryRecord(
    int TotalProducts,
    int InStockCount,
    int LowStockCount,
    int OutOfStockCount,
    decimal TotalStockValue);

public sealed record AdminFulfillmentRecord(DateTime OrderDate, DateTime? ShippedDate, DateTime? DeliveredDate);

public sealed record AdminUserFirstOrderRecord(string UserId, DateTime FirstOrder);

public sealed record AdminUserRevenueRecord(string UserId, decimal Total);

public sealed record AdminTopCustomerRecord(string UserId, decimal TotalSpent, int OrderCount, DateTime? LastOrderDate);

public sealed record AdminWishlistedRecord(int ProductId, string? Name, int WishlistCount);
