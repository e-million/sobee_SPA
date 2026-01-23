using Sobee.Domain.Data;

namespace Sobee.Domain.Repositories;

public sealed class SystemHealthRepository : ISystemHealthRepository
{
    private readonly SobeecoredbContext _db;

    public SystemHealthRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public Task<bool> CanConnectAsync()
        => _db.Database.CanConnectAsync();
}
