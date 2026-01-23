using Sobee.Domain.Entities.Promotions;

namespace Sobee.Domain.Repositories;

public interface IAdminPromoRepository
{
    Task<(IReadOnlyList<Tpromotion> Items, int TotalCount)> GetPromosAsync(
        string? search,
        bool includeExpired,
        int page,
        int pageSize);
    Task<IReadOnlyList<(string Code, int Count)>> GetUsageCountsAsync(IReadOnlyList<string> codes);
    Task<int> CountUsageAsync(string promoCode);
    Task<Tpromotion?> FindByIdAsync(int promoId, bool track = true);
    Task<bool> ExistsByCodeAsync(string code, int? excludePromoId = null);
    Task AddAsync(Tpromotion promo);
    Task RemoveAsync(Tpromotion promo);
    Task SaveChangesAsync();
}
