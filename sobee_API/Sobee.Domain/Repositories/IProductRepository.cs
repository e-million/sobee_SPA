using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public interface IProductRepository
{
    Task<Tproduct?> FindByIdAsync(int productId);
}
