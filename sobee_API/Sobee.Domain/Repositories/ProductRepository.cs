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

    public async Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> GetProductsAsync(
        string? query,
        string? category,
        int page,
        int pageSize,
        string? sort,
        bool track = false)
    {
        IQueryable<Tproduct> productsQuery = _db.Tproducts
            .Include(p => p.TproductImages)
            .Include(p => p.IntDrinkCategory);

        if (!track)
        {
            productsQuery = productsQuery.AsNoTracking();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            productsQuery = productsQuery.Where(p =>
                p.StrName.Contains(term) ||
                p.strDescription.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTerm = category.Trim();
            productsQuery = productsQuery.Where(p =>
                p.IntDrinkCategory != null &&
                p.IntDrinkCategory.StrName == categoryTerm);
        }

        var totalCount = await productsQuery.CountAsync();

        var isSqlite = _db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var requiresClientSort = isSqlite && (sort == "priceAsc" || sort == "priceDesc");
        List<Tproduct> products;

        if (requiresClientSort)
        {
            var allProducts = await productsQuery.ToListAsync();
            var ordered = sort == "priceAsc"
                ? allProducts.OrderBy(p => p.DecPrice).ThenBy(p => p.IntProductId)
                : allProducts.OrderByDescending(p => p.DecPrice).ThenBy(p => p.IntProductId);
            products = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            productsQuery = sort switch
            {
                "priceAsc" => productsQuery.OrderBy(p => p.DecPrice),
                "priceDesc" => productsQuery.OrderByDescending(p => p.DecPrice),
                _ => productsQuery.OrderBy(p => p.IntProductId)
            };

            products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        return (products, totalCount);
    }

    public async Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false)
    {
        var query = _db.Tproducts
            .Include(p => p.TproductImages)
            .Include(p => p.IntDrinkCategory)
            .AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(p => p.IntProductId == productId);
    }

    public async Task<bool> ExistsAsync(int productId)
    {
        return await _db.Tproducts.AnyAsync(p => p.IntProductId == productId);
    }

    public async Task<TproductImage?> FindImageAsync(int productId, int imageId)
    {
        return await _db.TproductImages
            .FirstOrDefaultAsync(i => i.IntProductImageId == imageId && i.IntProductId == productId);
    }

    public async Task AddAsync(Tproduct product)
    {
        _db.Tproducts.Add(product);
        await Task.CompletedTask;
    }

    public async Task AddImageAsync(TproductImage image)
    {
        _db.TproductImages.Add(image);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(Tproduct product)
    {
        _db.Tproducts.Remove(product);
        await Task.CompletedTask;
    }

    public async Task RemoveImageAsync(TproductImage image)
    {
        _db.TproductImages.Remove(image);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
