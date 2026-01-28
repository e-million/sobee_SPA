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

    public async Task<TdrinkCategory?> FindByIdAsync(int categoryId, bool track = true)
    {
        var query = _db.TdrinkCategories.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(c => c.IntDrinkCategoryId == categoryId);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var normalized = name.ToUpper();
        var query = _db.TdrinkCategories.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.IntDrinkCategoryId != excludeId.Value);
        }

        return await query.AnyAsync(c => c.StrName.ToUpper() == normalized);
    }

    public async Task<bool> HasProductsAsync(int categoryId)
    {
        return await _db.Tproducts.AnyAsync(p => p.IntDrinkCategoryId == categoryId);
    }

    public async Task AddAsync(TdrinkCategory category)
    {
        _db.TdrinkCategories.Add(category);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(TdrinkCategory category)
    {
        _db.TdrinkCategories.Remove(category);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
