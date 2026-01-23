using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Orders;

namespace Sobee.Domain.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly SobeecoredbContext _db;

    public OrderRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<Torder?> FindByIdAsync(int orderId, bool track = true)
    {
        return await BuildOrdersQuery(track)
            .FirstOrDefaultAsync(o => o.IntOrderId == orderId);
    }

    public async Task<Torder?> FindByIdWithItemsAsync(int orderId, bool track = true)
    {
        return await BuildOrdersWithItemsQuery(track)
            .FirstOrDefaultAsync(o => o.IntOrderId == orderId);
    }

    public async Task<Torder?> FindForOwnerAsync(int orderId, string? userId, string? sessionId, bool track = true)
    {
        var query = BuildOrdersQuery(track);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query = query.Where(o => o.SessionId == sessionId);
        }
        else
        {
            return null;
        }

        return await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);
    }

    public async Task<Torder?> FindForOwnerWithItemsAsync(int orderId, string? userId, string? sessionId, bool track = true)
    {
        var query = BuildOrdersWithItemsQuery(track);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(o => o.UserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query = query.Where(o => o.SessionId == sessionId);
        }
        else
        {
            return null;
        }

        return await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);
    }

    public async Task<IReadOnlyList<Torder>> GetUserOrdersAsync(string userId, int page, int pageSize)
    {
        return await BuildOrdersWithItemsQuery(track: false)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.DtmOrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountUserOrdersAsync(string userId)
    {
        return await BuildOrdersQuery(track: false)
            .Where(o => o.UserId == userId)
            .CountAsync();
    }

    public async Task AddAsync(Torder order)
    {
        _db.Torders.Add(order);
        await Task.CompletedTask;
    }

    public async Task AddItemsAsync(IEnumerable<TorderItem> items)
    {
        _db.TorderItems.AddRange(items);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync()
        => _db.Database.BeginTransactionAsync();

    private IQueryable<Torder> BuildOrdersQuery(bool track)
    {
        var query = _db.Torders.AsQueryable();
        return track ? query : query.AsNoTracking();
    }

    private IQueryable<Torder> BuildOrdersWithItemsQuery(bool track)
    {
        return BuildOrdersQuery(track)
            .Include(o => o.TorderItems)
            .ThenInclude(oi => oi.IntProduct);
    }
}
