using System;
using System.Linq;
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

    public async Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<Tproduct>();
        }

        var query = _db.Tproducts.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Where(p => ids.Contains(p.IntProductId))
            .ToListAsync();
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
