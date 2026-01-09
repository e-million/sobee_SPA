using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private const string SessionHeaderName = "X-Session-Id";
        private readonly SobeecoredbContext _db;

        public CartController(SobeecoredbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------
        // DTOs (request models)
        // ---------------------------------------------
        public class AddCartItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }

        public class UpdateCartItemRequest
        {
            public int Quantity { get; set; }
        }

        // ---------------------------------------------
        // GET: /api/cart
        // - If authenticated: uses UserId
        // - If guest: uses X-Session-Id (generates one if missing)
        // - If authenticated AND X-Session-Id exists: merges guest cart -> user cart
        // ---------------------------------------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCart()
        {
            var identity = ResolveIdentity(allowCreateSessionIdForGuest: true);

            // If guest and we generated a session id, return it so the client can store it
            if (identity.IsGuest && !string.IsNullOrWhiteSpace(identity.SessionId))
            {
                Response.Headers[SessionHeaderName] = identity.SessionId!;
            }

            var cart = await GetOrCreateCartAsync(identity.UserId, identity.SessionId);
            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ---------------------------------------------
        // POST: /api/cart/items
        // Body: { productId, quantity }
        // - Adds new item or increments existing
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpPost("items")]
        [AllowAnonymous]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.ProductId <= 0)
                return BadRequest(new { error = "ProductId must be a positive integer." });

            if (request.Quantity <= 0)
                return BadRequest(new { error = "Quantity must be greater than 0." });

            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);

            // Guests must supply X-Session-Id for write operations
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            // Ensure product exists
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == request.ProductId);
            if (!productExists)
                return NotFound(new { error = $"Product {request.ProductId} not found." });

            var cart = await GetOrCreateCartAsync(identity.UserId, identity.SessionId);

            // Find existing cart item for the product
            var existingItem = await _db.TcartItems.FirstOrDefaultAsync(i =>
                i.IntShoppingCartId == cart.IntShoppingCartId &&
                i.IntProductId == request.ProductId);

            if (existingItem == null)
            {
                var newItem = new TcartItem
                {
                    IntShoppingCartId = cart.IntShoppingCartId,
                    IntProductId = request.ProductId,
                    IntQuantity = request.Quantity,
                    DtmDateAdded = DateTime.UtcNow
                };

                _db.TcartItems.Add(newItem);
            }
            else
            {
                existingItem.IntQuantity = (existingItem.IntQuantity ?? 0) + request.Quantity;
            }

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Reload with products for response
            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ---------------------------------------------
        // PUT: /api/cart/items/{cartItemId}
        // Body: { quantity }
        // - Sets quantity (0 => delete item)
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpPut("items/{cartItemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.Quantity < 0)
                return BadRequest(new { error = "Quantity cannot be negative." });

            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            var cart = await FindCartAsync(identity.UserId, identity.SessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var item = await _db.TcartItems.FirstOrDefaultAsync(i =>
                i.IntCartItemId == cartItemId &&
                i.IntShoppingCartId == cart.IntShoppingCartId);

            if (item == null)
                return NotFound(new { error = $"Cart item {cartItemId} not found." });

            if (request.Quantity == 0)
            {
                _db.TcartItems.Remove(item);
            }
            else
            {
                item.IntQuantity = request.Quantity;
            }

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ---------------------------------------------
        // DELETE: /api/cart/items/{cartItemId}
        // - Removes one item
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpDelete("items/{cartItemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            var cart = await FindCartAsync(identity.UserId, identity.SessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var item = await _db.TcartItems.FirstOrDefaultAsync(i =>
                i.IntCartItemId == cartItemId &&
                i.IntShoppingCartId == cart.IntShoppingCartId);

            if (item == null)
                return NotFound(new { error = $"Cart item {cartItemId} not found." });

            _db.TcartItems.Remove(item);
            cart.DtmDateLastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ---------------------------------------------
        // DELETE: /api/cart
        // - Clears all items from cart (does NOT delete the cart row)
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpDelete]
        [AllowAnonymous]
        public async Task<IActionResult> ClearCart()
        {
            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            var cart = await FindCartAsync(identity.UserId, identity.SessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var items = await _db.TcartItems
                .Where(i => i.IntShoppingCartId == cart.IntShoppingCartId)
                .ToListAsync();

            _db.TcartItems.RemoveRange(items);

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ============================================================
        // Helpers
        // ============================================================

        private (bool IsGuest, string? UserId, string? SessionId) ResolveIdentity(bool allowCreateSessionIdForGuest)
        {
            // Always try to read session id (even for authenticated users)
            // so we can merge guest cart -> user cart right after login.
            string? sessionId = null;
            if (Request.Headers.TryGetValue(SessionHeaderName, out var values))
            {
                var raw = values.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                    sessionId = raw.Trim();
            }

            // Authenticated user
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    throw new InvalidOperationException("Authenticated request is missing NameIdentifier claim.");

                return (IsGuest: false, UserId: userId, SessionId: sessionId);
            }

            // Guest user
            if (string.IsNullOrWhiteSpace(sessionId) && allowCreateSessionIdForGuest)
            {
                sessionId = Guid.NewGuid().ToString();
            }

            return (IsGuest: true, UserId: null, SessionId: sessionId);
        }

        private async Task<TshoppingCart?> FindCartAsync(string? userId, string? sessionId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
                return await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);

            if (!string.IsNullOrWhiteSpace(sessionId))
                return await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.SessionId == sessionId);

            return null;
        }

        /// <summary>
        /// Returns the current cart, creating if needed.
        /// If authenticated AND a guest session cart exists, merges it into the user's cart.
        /// </summary>
        private async Task<TshoppingCart> GetOrCreateCartAsync(string? userId, string? sessionId)
        {
            TshoppingCart? userCart = null;
            TshoppingCart? sessionCart = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                userCart = await _db.TshoppingCarts
                    .Include(c => c.TcartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                sessionCart = await _db.TshoppingCarts
                    .Include(c => c.TcartItems)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);
            }

            // If user is logged in and has a session cart, merge session cart -> user cart
            if (!string.IsNullOrWhiteSpace(userId) && sessionCart != null)
            {
                if (userCart == null)
                {
                    // Claim the session cart as the user's cart
                    sessionCart.UserId = userId;
                    sessionCart.SessionId = null;
                    sessionCart.DtmDateLastUpdated = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    return await LoadCartWithItemsAsync(sessionCart.IntShoppingCartId);
                }

                // Merge items: add quantities into userCart
                foreach (var sessionItem in sessionCart.TcartItems.ToList())
                {
                    if (sessionItem.IntProductId == null) continue;

                    var existing = userCart.TcartItems
                        .FirstOrDefault(i => i.IntProductId == sessionItem.IntProductId);

                    if (existing == null)
                    {
                        userCart.TcartItems.Add(new TcartItem
                        {
                            IntShoppingCartId = userCart.IntShoppingCartId,
                            IntProductId = sessionItem.IntProductId,
                            IntQuantity = sessionItem.IntQuantity ?? 0,
                            DtmDateAdded = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.IntQuantity = (existing.IntQuantity ?? 0) + (sessionItem.IntQuantity ?? 0);
                    }
                }

                userCart.DtmDateLastUpdated = DateTime.UtcNow;

                // Remove old guest cart after merge
                _db.TshoppingCarts.Remove(sessionCart);

                await _db.SaveChangesAsync();
                return await LoadCartWithItemsAsync(userCart.IntShoppingCartId);
            }

            // No merge needed. Return existing cart or create one.
            if (userCart != null)
                return await LoadCartWithItemsAsync(userCart.IntShoppingCartId);

            if (sessionCart != null)
                return await LoadCartWithItemsAsync(sessionCart.IntShoppingCartId);

            var newCart = new TshoppingCart
            {
                UserId = userId,
                SessionId = sessionId,
                DtmDateCreated = DateTime.UtcNow,
                DtmDateLastUpdated = DateTime.UtcNow
            };

            _db.TshoppingCarts.Add(newCart);
            await _db.SaveChangesAsync();

            return await LoadCartWithItemsAsync(newCart.IntShoppingCartId);
        }

        private async Task<TshoppingCart> LoadCartWithItemsAsync(int cartId)
        {
            return await _db.TshoppingCarts
                .Include(c => c.TcartItems)
                    .ThenInclude(i => i.IntProduct)
                .FirstAsync(c => c.IntShoppingCartId == cartId);
        }

        private object ProjectCart(TshoppingCart cart, string? userId, string? sessionId)
        {
            var items = cart.TcartItems.Select(i => new
            {
                cartItemId = i.IntCartItemId,
                productId = i.IntProductId,
                quantity = i.IntQuantity,
                added = i.DtmDateAdded,
                product = i.IntProduct == null ? null : new
                {
                    id = i.IntProduct.IntProductId,
                    name = i.IntProduct.StrName,
                    description = i.IntProduct.strDescription,
                    price = i.IntProduct.DecPrice
                },
                lineTotal = (i.IntQuantity ?? 0) * (i.IntProduct?.DecPrice ?? 0m)
            }).ToList();

            var cartTotal = items.Sum(x => (decimal)x.lineTotal);

            return new
            {
                cartId = cart.IntShoppingCartId,
                owner = userId != null ? "user" : "guest",
                userId,
                sessionId,
                created = cart.DtmDateCreated,
                updated = cart.DtmDateLastUpdated,
                items,
                total = cartTotal
            };
        }
    }
}
