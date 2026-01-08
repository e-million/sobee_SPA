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
        // - Creates cart if missing
        // ---------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var identity = ResolveIdentity(allowCreateSessionIdForGuest: true);

            // If guest and we generated a session id, return it so the client can store it
            if (!string.IsNullOrWhiteSpace(identity.SessionId))
            {
                Response.Headers[SessionHeaderName] = identity.SessionId;
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
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.ProductId <= 0)
                return BadRequest(new { error = "ProductId must be a positive integer." });

            if (request.Quantity <= 0)
                return BadRequest(new { error = "Quantity must be greater than 0." });

            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            // Ensure product exists
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == request.ProductId);
            if (!productExists)
                return NotFound(new { error = $"Product {request.ProductId} not found." });

            var cart = await GetOrCreateCartAsync(identity.UserId, identity.SessionId);

            // Find existing cart item for the product
            var existingItem = await _db.TcartItems
                .FirstOrDefaultAsync(i => i.IntShoppingCartId == cart.IntShoppingCartId && i.IntProductId == request.ProductId);

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

            // Reload with includes so response has product info
            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);

            return Ok(ProjectCart(cart, identity.UserId, identity.SessionId));
        }

        // ---------------------------------------------
        // PUT: /api/cart/items/{cartItemId}
        // Body: { quantity }
        // - Updates quantity
        // - If quantity <= 0, removes item
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpPut("items/{cartItemId:int}")]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            var identity = ResolveIdentity(allowCreateSessionIdForGuest: false);
            if (identity.IsGuest && string.IsNullOrWhiteSpace(identity.SessionId))
                return BadRequest(new { error = $"Guest requests must include '{SessionHeaderName}' header." });

            // Load cart (must exist and belong to this user/session)
            var cart = await FindCartAsync(identity.UserId, identity.SessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var item = await _db.TcartItems.FirstOrDefaultAsync(i =>
                i.IntCartItemId == cartItemId &&
                i.IntShoppingCartId == cart.IntShoppingCartId);

            if (item == null)
                return NotFound(new { error = $"Cart item {cartItemId} not found." });

            if (request.Quantity <= 0)
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
        // - Removes item
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpDelete("items/{cartItemId:int}")]
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
        // - Clears all items from cart
        // - Guest requires X-Session-Id header
        // ---------------------------------------------
        [HttpDelete]
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
            // Authenticated user
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    // User is "authenticated" but missing expected claim -> treat as unauthorized
                    throw new InvalidOperationException("Authenticated request is missing NameIdentifier claim.");
                }

                return (IsGuest: false, UserId: userId, SessionId: null);
            }

            // Guest user
            string? sessionId = null;

            if (Request.Headers.TryGetValue(SessionHeaderName, out var values))
            {
                var raw = values.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                    sessionId = raw.Trim();
            }

            if (string.IsNullOrWhiteSpace(sessionId) && allowCreateSessionIdForGuest)
            {
                sessionId = Guid.NewGuid().ToString();
            }

            return (IsGuest: true, UserId: null, SessionId: sessionId);
        }

        private async Task<TshoppingCart?> FindCartAsync(string? userId, string? sessionId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            return null;
        }

        private async Task<TshoppingCart> GetOrCreateCartAsync(string? userId, string? sessionId)
        {
            TshoppingCart? cart = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                cart = await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.UserId == userId);
            }
            else if (!string.IsNullOrWhiteSpace(sessionId))
            {
                cart = await _db.TshoppingCarts.FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart != null)
            {
                // load full cart with items
                return await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            }

            cart = new TshoppingCart
            {
                UserId = userId,
                SessionId = sessionId,
                DtmDateCreated = DateTime.UtcNow,
                DtmDateLastUpdated = DateTime.UtcNow
            };

            _db.TshoppingCarts.Add(cart);
            await _db.SaveChangesAsync();

            return await LoadCartWithItemsAsync(cart.IntShoppingCartId);
        }

        private async Task<TshoppingCart> LoadCartWithItemsAsync(int cartId)
        {
            var cart = await _db.TshoppingCarts
                .Include(c => c.TcartItems)
                .ThenInclude(i => i.IntProduct)
                .FirstAsync(c => c.IntShoppingCartId == cartId);

            return cart;
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
                userId = userId,
                sessionId = sessionId,
                created = cart.DtmDateCreated,
                updated = cart.DtmDateLastUpdated,
                items = items,
                total = cartTotal
            };
        }
    }
}
