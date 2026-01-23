using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public interface IProductRepository
{
    Task<Tproduct?> FindByIdAsync(int productId);
    Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true);
    Task SaveChangesAsync();
}
