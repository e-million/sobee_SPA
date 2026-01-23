using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;

namespace Sobee.Domain.Repositories;

public sealed class GuestSessionRepository : IGuestSessionRepository
{
    private readonly SobeecoredbContext _db;

    public GuestSessionRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<GuestSession?> FindValidAsync(string sessionId, string secret, DateTime utcNow, bool track = true)
    {
        var query = _db.GuestSessions.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s =>
            s.SessionId == sessionId &&
            s.Secret == secret &&
            s.ExpiresAtUtc > utcNow);
    }

    public async Task<GuestSession?> FindByIdAsync(string sessionId, bool track = true)
    {
        var query = _db.GuestSessions.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.SessionId == sessionId);
    }

    public async Task AddAsync(GuestSession session)
    {
        _db.GuestSessions.Add(session);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(GuestSession session)
    {
        _db.GuestSessions.Remove(session);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
