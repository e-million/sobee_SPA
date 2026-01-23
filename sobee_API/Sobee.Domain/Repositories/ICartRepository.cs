using Sobee.Domain.Entities.Cart;

namespace Sobee.Domain.Repositories;

public interface ICartRepository
{
    Task<TshoppingCart?> FindByUserIdAsync(string userId);
    Task<TshoppingCart?> FindBySessionIdAsync(string sessionId);
    Task<TshoppingCart> CreateAsync(TshoppingCart cart);
    Task UpdateAsync(TshoppingCart cart);
    Task<TcartItem?> FindCartItemAsync(int cartId, int productId);
    Task<TcartItem?> FindCartItemByIdAsync(int cartItemId);
    Task AddCartItemAsync(TcartItem item);
    Task UpdateCartItemAsync(TcartItem item);
    Task RemoveCartItemAsync(TcartItem item);
    Task RemoveCartAsync(TshoppingCart cart);
    Task ClearCartItemsAsync(int cartId);
    Task<TshoppingCart> LoadCartWithItemsAsync(int cartId);
    Task SaveChangesAsync();
}
