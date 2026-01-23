namespace Sobee.Domain.Repositories;

public interface ISystemHealthRepository
{
    Task<bool> CanConnectAsync();
}
