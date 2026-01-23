using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;

namespace Sobee.Domain.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly SobeecoredbContext _db;

    public CartRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<TshoppingCart?> FindByUserIdAsync(string userId)
    {
        return await BuildCartQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<TshoppingCart?> FindBySessionIdAsync(string sessionId)
    {
        return await BuildCartQuery()
            .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);
    }

    public async Task<TshoppingCart> CreateAsync(TshoppingCart cart)
    {
        _db.TshoppingCarts.Add(cart);
        return cart;
    }

    public async Task UpdateAsync(TshoppingCart cart)
    {
        _db.TshoppingCarts.Update(cart);
        await Task.CompletedTask;
    }

    public async Task<TcartItem?> FindCartItemAsync(int cartId, int productId)
    {
        return await _db.TcartItems
            .FirstOrDefaultAsync(i => i.IntShoppingCartId == cartId && i.IntProductId == productId);
    }

    public async Task<TcartItem?> FindCartItemByIdAsync(int cartItemId)
    {
        return await _db.TcartItems
            .FirstOrDefaultAsync(i => i.IntCartItemId == cartItemId);
    }

    public async Task AddCartItemAsync(TcartItem item)
    {
        _db.TcartItems.Add(item);
        await Task.CompletedTask;
    }

    public async Task UpdateCartItemAsync(TcartItem item)
    {
        _db.TcartItems.Update(item);
        await Task.CompletedTask;
    }

    public async Task RemoveCartItemAsync(TcartItem item)
    {
        _db.TcartItems.Remove(item);
        await Task.CompletedTask;
    }

    public async Task RemoveCartAsync(TshoppingCart cart)
    {
        _db.TshoppingCarts.Remove(cart);
        await Task.CompletedTask;
    }

    public async Task ClearCartItemsAsync(int cartId)
    {
        var items = await _db.TcartItems
            .Where(i => i.IntShoppingCartId == cartId)
            .ToListAsync();

        if (items.Count == 0)
        {
            return;
        }

        _db.TcartItems.RemoveRange(items);
        await Task.CompletedTask;
    }

    public async Task<TshoppingCart> LoadCartWithItemsAsync(int cartId)
    {
        return await BuildCartQuery()
            .FirstAsync(c => c.IntShoppingCartId == cartId);
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();

    private IQueryable<TshoppingCart> BuildCartQuery()
    {
        return _db.TshoppingCarts
            .Include(c => c.TcartItems)
                .ThenInclude(i => i.IntProduct)
                    .ThenInclude(p => p.TproductImages);
    }
}
