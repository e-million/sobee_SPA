using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Promotions;

namespace Sobee.Domain.Repositories;

public sealed class AdminPromoRepository : IAdminPromoRepository
{
    private readonly SobeecoredbContext _db;

    public AdminPromoRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Tpromotion> Items, int TotalCount)> GetPromosAsync(
        string? search,
        bool includeExpired,
        int page,
        int pageSize)
    {
        var query = _db.Tpromotions.AsNoTracking();

        if (!includeExpired)
        {
            var today = DateTime.Today;
            query = query.Where(p => p.DtmExpirationDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.StrPromoCode.Contains(term));
        }

        var totalCount = await query.CountAsync();

        var promos = await query
            .OrderByDescending(p => p.DtmExpirationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (promos, totalCount);
    }

    public async Task<IReadOnlyList<(string Code, int Count)>> GetUsageCountsAsync(IReadOnlyList<string> codes)
    {
        if (codes.Count == 0)
        {
            return Array.Empty<(string, int)>();
        }

        var usageCounts = await _db.Torders
            .Where(o => o.StrPromoCode != null && codes.Contains(o.StrPromoCode))
            .GroupBy(o => o.StrPromoCode)
            .Select(g => new { Code = g.Key!, Count = g.Count() })
            .ToListAsync();

        return usageCounts.Select(x => (x.Code, x.Count)).ToList();
    }

    public async Task<int> CountUsageAsync(string promoCode)
    {
        return await _db.Torders.CountAsync(o => o.StrPromoCode == promoCode);
    }

    public async Task<Tpromotion?> FindByIdAsync(int promoId, bool track = true)
    {
        var query = _db.Tpromotions.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(p => p.IntPromotionId == promoId);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludePromoId = null)
    {
        var query = _db.Tpromotions.AsQueryable();
        if (excludePromoId.HasValue)
        {
            query = query.Where(p => p.IntPromotionId != excludePromoId.Value);
        }

        return await query.AnyAsync(p => p.StrPromoCode == code);
    }

    public async Task AddAsync(Tpromotion promo)
    {
        _db.Tpromotions.Add(promo);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(Tpromotion promo)
    {
        _db.Tpromotions.Remove(promo);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
