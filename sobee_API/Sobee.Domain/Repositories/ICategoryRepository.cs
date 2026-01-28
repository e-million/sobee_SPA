using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<TdrinkCategory>> GetCategoriesAsync();
}
