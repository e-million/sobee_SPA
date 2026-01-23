using Sobee.Domain.Entities.Promotions;

namespace Sobee.Domain.Repositories;

public interface IPromoRepository
{
    Task<Tpromotion?> FindActiveByCodeAsync(string promoCode, DateTime utcNow);
    Task<bool> UsageExistsAsync(int cartId, string promoCode);
    Task<IReadOnlyList<TpromoCodeUsageHistory>> GetUsagesForCartAsync(int cartId);
    Task AddUsageAsync(TpromoCodeUsageHistory usage);
    Task RemoveUsagesAsync(IEnumerable<TpromoCodeUsageHistory> usages);
    Task<(string? Code, decimal DiscountPercentage)> GetActivePromoForCartAsync(int cartId, DateTime utcNow);
    Task SaveChangesAsync();
}
