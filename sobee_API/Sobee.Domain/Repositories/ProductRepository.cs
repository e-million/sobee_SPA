using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly SobeecoredbContext _db;

    public ProductRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<Tproduct?> FindByIdAsync(int productId)
    {
        return await _db.Tproducts
            .FirstOrDefaultAsync(p => p.IntProductId == productId);
    }
}
