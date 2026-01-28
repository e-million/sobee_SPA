using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;

namespace Sobee.Domain.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly SobeecoredbContext _db;

    public CategoryRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TdrinkCategory>> GetCategoriesAsync()
    {
        return await _db.TdrinkCategories
            .AsNoTracking()
            .OrderBy(c => c.StrName)
            .ToListAsync();
    }
}
