using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<TdrinkCategory>> GetCategoriesAsync();
    Task<TdrinkCategory?> FindByIdAsync(int categoryId, bool track = true);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<bool> HasProductsAsync(int categoryId);
    Task AddAsync(TdrinkCategory category);
    Task RemoveAsync(TdrinkCategory category);
    Task SaveChangesAsync();
}
