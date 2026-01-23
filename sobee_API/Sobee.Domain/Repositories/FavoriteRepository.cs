using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Reviews;

namespace Sobee.Domain.Repositories;

public sealed class FavoriteRepository : IFavoriteRepository
{
    private readonly SobeecoredbContext _db;

    public FavoriteRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Tfavorite> Items, int TotalCount)> GetByUserAsync(string userId, int page, int pageSize)
    {
        var query = _db.Tfavorites
            .AsNoTracking()
            .Where(f => f.UserId == userId);

        var totalCount = await query.CountAsync();

        var favorites = await query
            .OrderByDescending(f => f.DtmDateAdded)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (favorites, totalCount);
    }

    public async Task<Tfavorite?> FindByUserAndProductAsync(string userId, int productId, bool track = true)
    {
        var query = _db.Tfavorites.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(f => f.UserId == userId && f.IntProductId == productId);
    }

    public async Task<bool> ExistsAsync(string userId, int productId)
    {
        return await _db.Tfavorites.AnyAsync(f => f.UserId == userId && f.IntProductId == productId);
    }

    public async Task AddAsync(Tfavorite favorite)
    {
        _db.Tfavorites.Add(favorite);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(Tfavorite favorite)
    {
        _db.Tfavorites.Remove(favorite);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
