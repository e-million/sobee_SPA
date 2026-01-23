using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public interface IProductRepository
{
    Task<Tproduct?> FindByIdAsync(int productId, bool track = true);
    Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true);
    Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> GetProductsAsync(
        string? query,
        string? category,
        int page,
        int pageSize,
        string? sort,
        bool track = false);
    Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false);
    Task<bool> ExistsAsync(int productId);
    Task<TproductImage?> FindImageAsync(int productId, int imageId);
    Task AddAsync(Tproduct product);
    Task AddImageAsync(TproductImage image);
    Task RemoveAsync(Tproduct product);
    Task RemoveImageAsync(TproductImage image);
    Task SaveChangesAsync();
}
