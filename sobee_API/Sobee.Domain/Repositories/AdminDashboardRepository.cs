using System;
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
        if (IsSqlite())
        {
            var totals = await _db.Torders
                .AsNoTracking()
                .Select(o => o.DecTotalAmount ?? 0m)
                .ToListAsync();
            return totals.Sum();
        }

        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DecTotalAmount != null)
            .SumAsync(o => o.DecTotalAmount) ?? 0m;
    }

    public async Task<decimal> GetTotalDiscountsAsync()
    {
        if (IsSqlite())
        {
            var totals = await _db.Torders
                .AsNoTracking()
                .Select(o => o.DecDiscountAmount ?? 0m)
                .ToListAsync();
            return totals.Sum();
        }

        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DecDiscountAmount != null)
            .SumAsync(o => o.DecDiscountAmount) ?? 0m;
    }

    public async Task<IReadOnlyList<AdminOrderDayRecord>> GetOrdersPerDayAsync(DateTime fromDate)
    {
        if (IsSqlite())
        {
            var orders = await _db.Torders
                .AsNoTracking()
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= fromDate)
                .Select(o => new
                {
                    Date = o.DtmOrderDate!.Value.Date,
                    Total = o.DecTotalAmount ?? 0m
                })
                .ToListAsync();

            return orders
                .GroupBy(o => o.Date)
                .Select(g => new AdminOrderDayRecord(
                    g.Key,
                    g.Count(),
                    g.Sum(x => x.Total)))
                .OrderBy(x => x.Date)
                .ToList();
        }

        var grouped = await _db.Torders
            .AsNoTracking()
            .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= fromDate)
            .GroupBy(o => new
            {
                Year = o.DtmOrderDate!.Value.Year,
                Month = o.DtmOrderDate!.Value.Month,
                Day = o.DtmOrderDate!.Value.Day
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                Count = g.Count(),
                Revenue = g.Sum(o => o.DecTotalAmount) ?? 0m
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ThenBy(x => x.Day)
            .ToListAsync();

        return grouped
            .Select(row => new AdminOrderDayRecord(
                new DateTime(row.Year, row.Month, row.Day),
                row.Count,
                row.Revenue))
            .ToList();
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
        if (IsSqlite())
        {
            var items_lite = await _db.TorderItems
                .AsNoTracking()
                .Where(i => i.IntProductId != null)
                .Select(i => new
                {
                    ProductId = i.IntProductId!.Value,
                    Name = i.IntProduct.StrName,
                    Quantity = i.IntQuantity ?? 0,
                    Revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
                })
                .ToListAsync();

            return items_lite
                .GroupBy(i => new { i.ProductId, i.Name })
                .Select(g => new AdminTopProductRecord(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Sum(x => x.Quantity),
                    g.Sum(x => x.Revenue)))
                .OrderByDescending(x => x.QuantitySold)
                .Take(limit)
                .ToList();
        }

        var items = await _db.TorderItems
            .AsNoTracking()
            .Where(i => i.IntProductId != null)
            .Select(i => new
            {
                ProductId = i.IntProductId!.Value,
                Name = i.IntProduct != null ? i.IntProduct.StrName : null,
                Quantity = i.IntQuantity ?? 0,
                Revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
            })
            .ToListAsync();

        return items
            .GroupBy(i => new { i.ProductId, i.Name })
            .Select(g => new AdminTopProductRecord(
                g.Key.ProductId,
                g.Key.Name,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Revenue)))
            .OrderByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToList();
    }

    private bool IsSqlite()
        => _db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
