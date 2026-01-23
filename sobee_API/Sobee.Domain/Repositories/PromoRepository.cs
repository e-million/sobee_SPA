using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Promotions;

namespace Sobee.Domain.Repositories;

public sealed class PromoRepository : IPromoRepository
{
    private readonly SobeecoredbContext _db;

    public PromoRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<Tpromotion?> FindActiveByCodeAsync(string promoCode, DateTime utcNow)
    {
        return await _db.Tpromotions
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.StrPromoCode == promoCode &&
                p.DtmExpirationDate > utcNow);
    }

    public async Task<bool> UsageExistsAsync(int cartId, string promoCode)
    {
        return await _db.TpromoCodeUsageHistories
            .AsNoTracking()
            .AnyAsync(p =>
                p.IntShoppingCartId == cartId &&
                p.PromoCode == promoCode);
    }

    public async Task<IReadOnlyList<TpromoCodeUsageHistory>> GetUsagesForCartAsync(int cartId)
    {
        return await _db.TpromoCodeUsageHistories
            .AsNoTracking()
            .Where(p => p.IntShoppingCartId == cartId)
            .ToListAsync();
    }

    public async Task AddUsageAsync(TpromoCodeUsageHistory usage)
    {
        _db.TpromoCodeUsageHistories.Add(usage);
        await Task.CompletedTask;
    }

    public async Task RemoveUsagesAsync(IEnumerable<TpromoCodeUsageHistory> usages)
    {
        _db.TpromoCodeUsageHistories.RemoveRange(usages);
        await Task.CompletedTask;
    }

    public async Task<(string? Code, decimal DiscountPercentage)> GetActivePromoForCartAsync(int cartId, DateTime utcNow)
    {
        var promo = await _db.TpromoCodeUsageHistories
            .AsNoTracking()
            .Join(_db.Tpromotions.AsNoTracking(),
                usage => usage.PromoCode,
                promotion => promotion.StrPromoCode,
                (usage, promotion) => new { usage, promotion })
            .Where(x => x.usage.IntShoppingCartId == cartId &&
                        x.promotion.DtmExpirationDate > utcNow)
            .OrderByDescending(x => x.usage.UsedDateTime)
            .Select(x => new { x.promotion.StrPromoCode, x.promotion.DecDiscountPercentage })
            .FirstOrDefaultAsync();

        if (promo == null)
        {
            return (null, 0m);
        }

        return (promo.StrPromoCode, promo.DecDiscountPercentage);
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
