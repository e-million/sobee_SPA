using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;

namespace Sobee.Domain.Repositories;

public sealed class AdminDashboardRepository : IAdminDashboardRepository
{
    private readonly SobeecoredbContext _db;

    public AdminDashboardRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<int> GetTotalOrdersAsync()
        => await _db.Torders.AsNoTracking().CountAsync();

    public async Task<decimal> GetTotalRevenueAsync()
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DecTotalAmount != null)
            .SumAsync(o => o.DecTotalAmount) ?? 0m;
    }

    public async Task<decimal> GetTotalDiscountsAsync()
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DecDiscountAmount != null)
            .SumAsync(o => o.DecDiscountAmount) ?? 0m;
    }

    public async Task<IReadOnlyList<AdminOrderDayRecord>> GetOrdersPerDayAsync(DateTime fromDate)
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= fromDate)
            .GroupBy(o => o.DtmOrderDate!.Value.Date)
            .Select(g => new AdminOrderDayRecord(
                g.Key,
                g.Count(),
                g.Sum(o => o.DecTotalAmount) ?? 0m))
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminLowStockRecord>> GetLowStockAsync(int threshold)
    {
        return await _db.Tproducts
            .AsNoTracking()
            .Where(p => p.IntStockAmount <= threshold)
            .OrderBy(p => p.IntStockAmount)
            .Select(p => new AdminLowStockRecord(
                p.IntProductId,
                p.StrName,
                p.IntStockAmount))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminTopProductRecord>> GetTopProductsAsync(int limit)
    {
        return await _db.TorderItems
            .AsNoTracking()
            .GroupBy(i => new
            {
                i.IntProductId,
                i.IntProduct.StrName
            })
            .Select(g => new AdminTopProductRecord(
                (int)g.Key.IntProductId,
                g.Key.StrName,
                g.Sum(x => x.IntQuantity ?? 0),
                g.Sum(x => (x.IntQuantity ?? 0) * (x.MonPricePerUnit ?? 0m))))
            .OrderByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync();
    }
}
