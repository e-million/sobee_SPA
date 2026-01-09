using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using sobee_API.Services;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly SobeecoredbContext _db;
        private readonly GuestSessionService _guestSessionService;
        private readonly RequestIdentityResolver _identityResolver;

        public CartController(
            SobeecoredbContext db,
            GuestSessionService guestSessionService,
            RequestIdentityResolver identityResolver)
        {
            _db = db;
            _guestSessionService = guestSessionService;
            _identityResolver = identityResolver;
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
        // - If guest: uses X-Session-Id + X-Session-Secret (generates both if missing/invalid)
        // - If authenticated AND validated guest session exists: merges guest cart -> user cart
        // ---------------------------------------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCart()
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
            {
                return errorResult;
            }

            var cart = await GetOrCreateCartAsync(identity!.UserId, identity.GuestSessionId, identity.GuestSessionValidated);
            return Ok(ProjectCart(cart, identity.UserId, identity.GuestSessionId));
        }

        // ---------------------------------------------
        // POST: /api/cart/items
        // Body: { productId, quantity }
        // - Adds new item or increments existing
        // - Guest uses X-Session-Id + X-Session-Secret headers (new session issued if missing/invalid)
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

            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
            {
                return errorResult;
            }

            // Ensure product exists
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == request.ProductId);
            if (!productExists)
                return NotFound(new { error = $"Product {request.ProductId} not found." });

            var cart = await GetOrCreateCartAsync(identity!.UserId, identity.GuestSessionId, identity.GuestSessionValidated);

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
            return Ok(ProjectCart(cart, identity.UserId, identity.GuestSessionId));
        }

        // ---------------------------------------------
        // PUT: /api/cart/items/{cartItemId}
        // Body: { quantity }
        // - Sets quantity (0 => delete item)
        // - Guest uses X-Session-Id + X-Session-Secret headers
        // ---------------------------------------------
        [HttpPut("items/{cartItemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.Quantity < 0)
                return BadRequest(new { error = "Quantity cannot be negative." });

            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
            {
                return errorResult;
            }

            var cart = await FindCartAsync(identity!.UserId, identity.GuestSessionId);
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
            return Ok(ProjectCart(cart, identity.UserId, identity.GuestSessionId));
        }

        // ---------------------------------------------
        // DELETE: /api/cart/items/{cartItemId}
        // - Removes one item
        // - Guest uses X-Session-Id + X-Session-Secret headers
        // ---------------------------------------------
        [HttpDelete("items/{cartItemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
            {
                return errorResult;
            }

            var cart = await FindCartAsync(identity!.UserId, identity.GuestSessionId);
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
            return Ok(ProjectCart(cart, identity.UserId, identity.GuestSessionId));
        }

        // ---------------------------------------------
        // DELETE: /api/cart
        // - Clears all items from cart (does NOT delete the cart row)
        // - Guest uses X-Session-Id + X-Session-Secret headers
        // ---------------------------------------------
        [HttpDelete]
        [AllowAnonymous]
        public async Task<IActionResult> ClearCart()
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
            {
                return errorResult;
            }

            var cart = await FindCartAsync(identity!.UserId, identity.GuestSessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var items = await _db.TcartItems
                .Where(i => i.IntShoppingCartId == cart.IntShoppingCartId)
                .ToListAsync();

            _db.TcartItems.RemoveRange(items);

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(ProjectCart(cart, identity.UserId, identity.GuestSessionId));
        }

        // ============================================================
        // Helpers
        // ============================================================

        private async Task<(RequestIdentity? identity, IActionResult? errorResult)> ResolveIdentityAsync(bool allowCreateGuestSession)
        {
            var identity = await _identityResolver.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession,
                allowAuthenticatedGuestSession: true);


            if (identity.HasError)
            {
                if (identity.ErrorCode == "MissingNameIdentifier")
                {
                    return (null, Unauthorized(new { error = identity.ErrorMessage }));
                }

                return (null, BadRequest(new { error = identity.ErrorMessage }));
            }

            return (identity, null);
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
        private async Task<TshoppingCart> GetOrCreateCartAsync(string? userId, string? sessionId, bool canMergeGuestSession)
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
            if (!string.IsNullOrWhiteSpace(userId) && sessionCart != null && canMergeGuestSession)
            {
                if (userCart == null)
                {
                    // Claim the session cart as the user's cart
                    sessionCart.UserId = userId;
                    sessionCart.SessionId = null;
                    sessionCart.DtmDateLastUpdated = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    await RotateGuestSessionAsync(sessionId);
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
                await RotateGuestSessionAsync(sessionId);
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

        private async Task RotateGuestSessionAsync(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            // Rotate to prevent guest session fixation reuse after merge.
            await _guestSessionService.InvalidateAsync(sessionId);
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
