using Sobee.Domain.Entities.Cart;

namespace Sobee.Domain.Repositories;

public interface IGuestSessionRepository
{
    Task<GuestSession?> FindValidAsync(string sessionId, string secret, DateTime utcNow, bool track = true);
    Task<GuestSession?> FindByIdAsync(string sessionId, bool track = true);
    Task AddAsync(GuestSession session);
    Task RemoveAsync(GuestSession session);
    Task SaveChangesAsync();
}
