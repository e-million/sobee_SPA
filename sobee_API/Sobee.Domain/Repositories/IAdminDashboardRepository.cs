namespace Sobee.Domain.Repositories;

public interface IAdminDashboardRepository
{
    Task<int> GetTotalOrdersAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetTotalDiscountsAsync();
    Task<IReadOnlyList<AdminOrderDayRecord>> GetOrdersPerDayAsync(DateTime fromDate);
    Task<IReadOnlyList<AdminLowStockRecord>> GetLowStockAsync(int threshold);
    Task<IReadOnlyList<AdminTopProductRecord>> GetTopProductsAsync(int limit);
}

public sealed record AdminOrderDayRecord(DateTime Date, int Count, decimal Revenue);

public sealed record AdminLowStockRecord(int ProductId, string? Name, int StockAmount);

public sealed record AdminTopProductRecord(int ProductId, string? Name, int QuantitySold, decimal Revenue);
