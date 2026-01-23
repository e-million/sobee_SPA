using sobee_API.Domain;
using sobee_API.DTOs.Admin;

namespace sobee_API.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<ServiceResult<AdminSummaryResponse>> GetSummaryAsync();
    Task<ServiceResult<IReadOnlyList<OrdersPerDayResponse>>> GetOrdersPerDayAsync(int days);
    Task<ServiceResult<IReadOnlyList<LowStockProductResponse>>> GetLowStockAsync(int threshold);
    Task<ServiceResult<IReadOnlyList<TopProductResponse>>> GetTopProductsAsync(int limit);
}
