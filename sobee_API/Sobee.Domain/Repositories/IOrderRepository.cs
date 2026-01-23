using Microsoft.EntityFrameworkCore.Storage;
using Sobee.Domain.Entities.Orders;

namespace Sobee.Domain.Repositories;

public interface IOrderRepository
{
    Task<Torder?> FindByIdAsync(int orderId, bool track = true);
    Task<Torder?> FindByIdWithItemsAsync(int orderId, bool track = true);
    Task<Torder?> FindForOwnerAsync(int orderId, string? userId, string? sessionId, bool track = true);
    Task<Torder?> FindForOwnerWithItemsAsync(int orderId, string? userId, string? sessionId, bool track = true);
    Task<IReadOnlyList<Torder>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<int> CountUserOrdersAsync(string userId);
    Task AddAsync(Torder order);
    Task AddItemsAsync(IEnumerable<TorderItem> items);
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}
