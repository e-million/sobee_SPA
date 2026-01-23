using System;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;

namespace Sobee.Domain.Repositories;

public sealed class AdminAnalyticsRepository : IAdminAnalyticsRepository
{
    private readonly SobeecoredbContext _db;

    public AdminAnalyticsRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminRevenueRow>> GetRevenueByDayAsync(DateTime start, DateTime end)
    {
        if (IsSqlite())
        {
            var orders = await _db.Torders
                .AsNoTracking()
                .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
                .Select(o => new
                {
                    Date = o.DtmOrderDate!.Value.Date,
                    Total = o.DecTotalAmount ?? 0m
                })
                .ToListAsync();

            return orders
                .GroupBy(o => o.Date)
                .Select(g => new AdminRevenueRow(
                    g.Key,
                    g.Sum(x => x.Total),
                    g.Count(),
                    g.Count() == 0 ? 0m : g.Sum(x => x.Total) / g.Count()))
                .OrderBy(x => x.Date)
                .ToList();
        }

        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
            .GroupBy(o => o.DtmOrderDate!.Value.Date)
            .Select(g => new AdminRevenueRow(
                g.Key,
                g.Sum(x => x.DecTotalAmount ?? 0m),
                g.Count(),
                g.Count() == 0 ? 0m : g.Sum(x => x.DecTotalAmount ?? 0m) / g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminRevenueRawRecord>> GetRevenueRawAsync(DateTime start, DateTime end)
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
            .Select(o => new AdminRevenueRawRecord(
                o.DtmOrderDate!.Value,
                o.DecTotalAmount ?? 0m))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string?>> GetOrderStatusesAsync()
    {
        return await _db.Torders
            .AsNoTracking()
            .Select(o => o.StrOrderStatus)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetReviewRatingsAsync()
    {
        return await _db.Treviews
            .AsNoTracking()
            .Select(r => r.IntRating)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminRecentReviewRecord>> GetRecentReviewsAsync(int limit)
    {
        return await _db.Treviews
            .AsNoTracking()
            .OrderByDescending(r => r.DtmReviewDate)
            .Take(limit)
            .Select(r => new AdminRecentReviewRecord(
                r.IntReviewId,
                r.IntProductId,
                r.IntProduct.StrName,
                r.IntRating,
                r.StrReviewText,
                r.DtmReviewDate,
                r.UserId,
                r.TReviewReplies.Any()))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminWorstProductRecord>> GetWorstProductsAsync(int limit)
    {
        if (IsSqlite())
        {
            var products = await _db.Tproducts
                .AsNoTracking()
                .Select(p => new { p.IntProductId, p.StrName })
                .ToListAsync();

            var items = await _db.TorderItems
                .AsNoTracking()
                .Where(i => i.IntProductId != null)
                .Select(i => new
                {
                    ProductId = i.IntProductId!.Value,
                    Quantity = i.IntQuantity ?? 0,
                    Revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
                })
                .ToListAsync();

            var totals = items
                .GroupBy(i => i.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => new { Units = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Revenue) });

            return products
                .Select(p =>
                {
                    totals.TryGetValue(p.IntProductId, out var data);
                    return new AdminWorstProductRecord(
                        p.IntProductId,
                        p.StrName,
                        data?.Units ?? 0,
                        data?.Revenue ?? 0m);
                })
                .OrderBy(p => p.UnitsSold)
                .ThenBy(p => p.Name)
                .Take(limit)
                .ToList();
        }

        return await _db.Tproducts
            .AsNoTracking()
            .Select(p => new AdminWorstProductRecord(
                p.IntProductId,
                p.StrName,
                p.TorderItems.Sum(i => (int?)i.IntQuantity) ?? 0,
                p.TorderItems.Sum(i => (decimal?)((i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m))) ?? 0m))
            .OrderBy(p => p.UnitsSold)
            .ThenBy(p => p.Name)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminCategoryProductCountRecord>> GetCategoryProductCountsAsync()
    {
        return await _db.Tproducts
            .AsNoTracking()
            .Include(p => p.IntDrinkCategory)
            .GroupBy(p => new
            {
                p.IntDrinkCategoryId,
                categoryName = p.IntDrinkCategory != null ? p.IntDrinkCategory.StrName : null
            })
            .Select(g => new AdminCategoryProductCountRecord(
                g.Key.IntDrinkCategoryId,
                g.Key.categoryName,
                g.Count()))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminCategorySalesRecord>> GetCategorySalesAsync(DateTime start, DateTime end)
    {
        if (IsSqlite())
        {
            var items = await _db.TorderItems
                .AsNoTracking()
                .Where(i => i.IntOrder != null
                    && i.IntOrder.DtmOrderDate != null
                    && i.IntOrder.DtmOrderDate >= start
                    && i.IntOrder.DtmOrderDate <= end)
                .Select(i => new
                {
                    categoryId = i.IntProduct.IntDrinkCategoryId,
                    categoryName = i.IntProduct.IntDrinkCategory != null ? i.IntProduct.IntDrinkCategory.StrName : null,
                    unitsSold = i.IntQuantity ?? 0,
                    revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
                })
                .ToListAsync();

            return items
                .GroupBy(item => new { item.categoryId, item.categoryName })
                .Select(g => new AdminCategorySalesRecord(
                    g.Key.categoryId,
                    g.Key.categoryName,
                    g.Sum(x => x.unitsSold),
                    g.Sum(x => x.revenue)))
                .ToList();
        }

        return await _db.TorderItems
            .AsNoTracking()
            .Where(i => i.IntOrder != null
                && i.IntOrder.DtmOrderDate != null
                && i.IntOrder.DtmOrderDate >= start
                && i.IntOrder.DtmOrderDate <= end)
            .Select(i => new
            {
                categoryId = i.IntProduct.IntDrinkCategoryId,
                categoryName = i.IntProduct.IntDrinkCategory != null ? i.IntProduct.IntDrinkCategory.StrName : null,
                unitsSold = i.IntQuantity ?? 0,
                revenue = (i.IntQuantity ?? 0) * (i.MonPricePerUnit ?? 0m)
            })
            .GroupBy(item => new { item.categoryId, item.categoryName })
            .Select(g => new AdminCategorySalesRecord(
                g.Key.categoryId,
                g.Key.categoryName,
                g.Sum(x => x.unitsSold),
                g.Sum(x => x.revenue)))
            .ToListAsync();
    }

    public async Task<AdminInventorySummaryRecord> GetInventorySummaryAsync(int lowStockThreshold)
    {
        var totalProducts = await _db.Tproducts.CountAsync();
        var outOfStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount <= 0);
        var inStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount > 0);
        var lowStockCount = await _db.Tproducts.CountAsync(p => p.IntStockAmount > 0 && p.IntStockAmount <= lowStockThreshold);
        var isSqlite = IsSqlite();
        decimal totalStockValue;

        if (isSqlite)
        {
            var products = await _db.Tproducts
                .AsNoTracking()
                .Select(p => new { p.DecCost, p.IntStockAmount })
                .ToListAsync();
            totalStockValue = products.Sum(p => (p.DecCost ?? 0m) * p.IntStockAmount);
        }
        else
        {
            totalStockValue = await _db.Tproducts
                .SumAsync(p => (p.DecCost ?? 0m) * p.IntStockAmount);
        }

        return new AdminInventorySummaryRecord(
            totalProducts,
            inStockCount,
            lowStockCount,
            outOfStockCount,
            totalStockValue);
    }

    public async Task<IReadOnlyList<AdminFulfillmentRecord>> GetFulfillmentOrdersAsync(DateTime start, DateTime end)
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
            .Select(o => new AdminFulfillmentRecord(
                o.DtmOrderDate!.Value,
                o.DtmShippedDate,
                o.DtmDeliveredDate))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminUserFirstOrderRecord>> GetFirstOrdersByUserAsync()
    {
        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.UserId != null && o.DtmOrderDate != null)
            .GroupBy(o => o.UserId!)
            .Select(g => new AdminUserFirstOrderRecord(
                g.Key,
                g.Min(x => x.DtmOrderDate)!.Value))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminUserRevenueRecord>> GetUserRevenueInRangeAsync(DateTime start, DateTime end)
    {
        if (IsSqlite())
        {
            var orders = await _db.Torders
                .AsNoTracking()
                .Where(o => o.UserId != null && o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
                .Select(o => new { o.UserId, Total = o.DecTotalAmount ?? 0m })
                .ToListAsync();

            return orders
                .GroupBy(o => o.UserId!)
                .Select(g => new AdminUserRevenueRecord(
                    g.Key,
                    g.Sum(x => x.Total)))
                .ToList();
        }

        return await _db.Torders
            .AsNoTracking()
            .Where(o => o.UserId != null && o.DtmOrderDate != null && o.DtmOrderDate >= start && o.DtmOrderDate <= end)
            .GroupBy(o => o.UserId!)
            .Select(g => new AdminUserRevenueRecord(
                g.Key,
                g.Sum(x => x.DecTotalAmount ?? 0m)))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminTopCustomerRecord>> GetTopCustomersAsync(int limit, DateTime? start, DateTime? end)
    {
        var query = _db.Torders
            .AsNoTracking()
            .Where(o => !string.IsNullOrEmpty(o.UserId));

        if (start.HasValue || end.HasValue)
        {
            var effectiveStart = start ?? DateTime.MinValue;
            var effectiveEnd = end ?? DateTime.MaxValue;
            query = query.Where(o => o.DtmOrderDate != null && o.DtmOrderDate >= effectiveStart && o.DtmOrderDate <= effectiveEnd);
        }

        if (IsSqlite())
        {
            var orders = await query
                .Select(o => new
                {
                    o.UserId,
                    Total = o.DecTotalAmount ?? 0m,
                    o.DtmOrderDate
                })
                .ToListAsync();

            return orders
                .GroupBy(o => o.UserId!)
                .Select(g => new AdminTopCustomerRecord(
                    g.Key,
                    g.Sum(x => x.Total),
                    g.Count(),
                    g.Max(x => x.DtmOrderDate)))
                .OrderByDescending(x => x.TotalSpent)
                .Take(limit)
                .ToList();
        }

        return await query
            .GroupBy(o => o.UserId!)
            .Select(g => new AdminTopCustomerRecord(
                g.Key,
                g.Sum(o => o.DecTotalAmount ?? 0m),
                g.Count(),
                g.Max(o => o.DtmOrderDate)))
            .OrderByDescending(x => x.TotalSpent)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminWishlistedRecord>> GetMostWishlistedAsync(int limit)
    {
        if (IsSqlite())
        {
            var favorites = await _db.Tfavorites
                .AsNoTracking()
                .Select(f => new { f.IntProductId, f.IntProduct.StrName })
                .ToListAsync();

            return favorites
                .GroupBy(f => new { f.IntProductId, f.StrName })
                .Select(g => new AdminWishlistedRecord(
                    g.Key.IntProductId,
                    g.Key.StrName,
                    g.Count()))
                .OrderByDescending(x => x.WishlistCount)
                .Take(limit)
                .ToList();
        }

        return await _db.Tfavorites
            .AsNoTracking()
            .GroupBy(f => new { f.IntProductId, f.IntProduct.StrName })
            .Select(g => new AdminWishlistedRecord(
                g.Key.IntProductId,
                g.Key.StrName,
                g.Count()))
            .OrderByDescending(x => x.WishlistCount)
            .Take(limit)
            .ToListAsync();
    }

    private bool IsSqlite()
        => _db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
}
