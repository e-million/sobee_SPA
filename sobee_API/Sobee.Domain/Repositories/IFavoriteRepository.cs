using Sobee.Domain.Entities.Reviews;

namespace Sobee.Domain.Repositories;

public interface IFavoriteRepository
{
    Task<(IReadOnlyList<Tfavorite> Items, int TotalCount)> GetByUserAsync(string userId, int page, int pageSize);
    Task<Tfavorite?> FindByUserAndProductAsync(string userId, int productId, bool track = true);
    Task<bool> ExistsAsync(string userId, int productId);
    Task AddAsync(Tfavorite favorite);
    Task RemoveAsync(Tfavorite favorite);
    Task SaveChangesAsync();
}
