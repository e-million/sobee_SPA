using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Promotions;
using sobee_API.DTOs;
using sobee_API.DTOs.Cart;
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

        /// <summary>
        /// Get the current cart for the authenticated user or guest session (creates/merges when needed).
        /// </summary>
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
            return Ok(await ProjectCartAsync(cart, identity.UserId, identity.GuestSessionId));
        }

        /// <summary>
        /// Add an item to the cart (increments quantity if already present).
        /// </summary>
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
                return errorResult;

            // Load product (needed for stock check)
            var product = await _db.Tproducts
                .FirstOrDefaultAsync(p => p.IntProductId == request.ProductId);

            if (product == null)
                return NotFound(new { error = $"Product {request.ProductId} not found." });

            var cart = await GetOrCreateCartAsync(
                identity!.UserId,
                identity.GuestSessionId,
                identity.GuestSessionValidated);

            // Find existing cart item
            var existingItem = await _db.TcartItems.FirstOrDefaultAsync(i =>
                i.IntShoppingCartId == cart.IntShoppingCartId &&
                i.IntProductId == request.ProductId);

            if (existingItem == null)
            {
                // Stock check for new item
                if (request.Quantity > product.IntStockAmount)
                    return Conflict(new
                    {
                        error = "Insufficient stock.",
                        productId = product.IntProductId,
                        availableStock = product.IntStockAmount
                    });

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
                var newQuantity = (existingItem.IntQuantity ?? 0) + request.Quantity;

                // Stock check for increment
                if (newQuantity > product.IntStockAmount)
                    return Conflict(new
                    {
                        error = "Insufficient stock.",
                        productId = product.IntProductId,
                        availableStock = product.IntStockAmount
                    });

                existingItem.IntQuantity = newQuantity;
            }

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(await ProjectCartAsync(cart, identity.UserId, identity.GuestSessionId));
        }

        /// <summary>
        /// Apply a promo code to the current cart.
        /// </summary>
        [HttpPost("promo/apply")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PromoCode))
                return BadRequest(new { error = "PromoCode is required." });

            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
                return errorResult;

            var cart = await FindCartAsync(identity!.UserId, identity.GuestSessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var promoCode = request.PromoCode.Trim();

            var promo = await _db.Tpromotions.FirstOrDefaultAsync(p =>
                p.StrPromoCode == promoCode &&
                p.DtmExpirationDate > DateTime.UtcNow);

            if (promo == null)
                return BadRequest(new { error = "Invalid or expired promo code." });

            var alreadyApplied = await _db.TpromoCodeUsageHistories.AnyAsync(p =>
                p.IntShoppingCartId == cart.IntShoppingCartId &&
                p.PromoCode == promoCode);

            if (alreadyApplied)
                return Conflict(new { error = "Promo code already applied to this cart." });

            _db.TpromoCodeUsageHistories.Add(new TpromoCodeUsageHistory
            {
                IntShoppingCartId = cart.IntShoppingCartId,
                PromoCode = promoCode,
                UsedDateTime = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Promo code applied.",
                promoCode,
                discountPercentage = promo.DecDiscountPercentage
            });
        }

        /// <summary>
        /// Update a cart item's quantity (0 removes the item).
        /// </summary>
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
                return errorResult;

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
                // Load product for stock validation
                var product = await _db.Tproducts
                    .FirstOrDefaultAsync(p => p.IntProductId == item.IntProductId);

                if (product == null)
                    return NotFound(new { error = $"Product {item.IntProductId} not found." });

                if (request.Quantity > product.IntStockAmount)
                    return Conflict(new
                    {
                        error = "Insufficient stock.",
                        productId = product.IntProductId,
                        availableStock = product.IntStockAmount
                    });

                item.IntQuantity = request.Quantity;
            }

            cart.DtmDateLastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            cart = await LoadCartWithItemsAsync(cart.IntShoppingCartId);
            return Ok(await ProjectCartAsync(cart, identity.UserId, identity.GuestSessionId));
        }

        /// <summary>
        /// Remove a cart item from the current cart.
        /// </summary>
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
            return Ok(await ProjectCartAsync(cart, identity.UserId, identity.GuestSessionId));
        }

        /// <summary>
        /// Clear all items from the current cart.
        /// </summary>
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
            return Ok(await ProjectCartAsync(cart, identity.UserId, identity.GuestSessionId));
        }
        /// <summary>
        /// Remove the promo code applied to the current cart.
        /// </summary>
        [HttpDelete("promo")]
        [AllowAnonymous]
        public async Task<IActionResult> RemovePromo()
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
                return errorResult;

            var cart = await FindCartAsync(identity!.UserId, identity.GuestSessionId);
            if (cart == null)
                return NotFound(new { error = "Cart not found." });

            var promos = await _db.TpromoCodeUsageHistories
                .Where(p => p.IntShoppingCartId == cart.IntShoppingCartId)
                .ToListAsync();

            if (promos.Count == 0)
                return BadRequest(new { error = "No promo code applied to cart." });

            _db.TpromoCodeUsageHistories.RemoveRange(promos);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Promo code removed." });
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

        private async Task<CartResponseDto> ProjectCartAsync(TshoppingCart cart, string? userId, string? sessionId)
        {
            var items = cart.TcartItems.Select(i => new CartItemResponseDto
            {
                CartItemId = i.IntCartItemId,
                ProductId = i.IntProductId,
                Quantity = i.IntQuantity,
                Added = i.DtmDateAdded,
                Product = i.IntProduct == null ? null : new CartProductDto
                {
                    Id = i.IntProduct.IntProductId,
                    Name = i.IntProduct.StrName,
                    Description = i.IntProduct.strDescription,
                    Price = i.IntProduct.DecPrice
                },
                LineTotal = (i.IntQuantity ?? 0) * (i.IntProduct?.DecPrice ?? 0m)
            }).ToList();

            var subtotal = items.Sum(x => x.LineTotal);

            // -------------------------------------------------
            // Promo (cart-scoped, most recently applied)
            // -------------------------------------------------
            var promo = await GetActivePromoForCartAsync(cart.IntShoppingCartId);

            var discountAmount = 0m;
            if (promo.DiscountPercentage > 0 && subtotal > 0)
            {
                discountAmount = subtotal * (promo.DiscountPercentage / 100m);
            }

            var total = subtotal - discountAmount;
            if (total < 0)
                total = 0;

            return new CartResponseDto
            {
                CartId = cart.IntShoppingCartId,
                Owner = userId != null ? "user" : "guest",
                UserId = userId,
                SessionId = sessionId,
                Created = cart.DtmDateCreated,
                Updated = cart.DtmDateLastUpdated,
                Items = items,
                Promo = promo.Code == null ? null : new CartPromoDto
                {
                    Code = promo.Code,
                    DiscountPercentage = promo.DiscountPercentage
                },
                Subtotal = subtotal,
                Discount = discountAmount,
                Total = total
            };
        }

        private async Task<(string? Code, decimal DiscountPercentage)> GetActivePromoForCartAsync(int cartId)
        {
            // Most recently applied promo code for this cart
            var promo = await _db.TpromoCodeUsageHistories
                .Join(_db.Tpromotions,
                    usage => usage.PromoCode,
                    promo => promo.StrPromoCode,
                    (usage, promo) => new { usage, promo })
                .Where(x => x.usage.IntShoppingCartId == cartId &&
                            x.promo.DtmExpirationDate > DateTime.UtcNow)
                .OrderByDescending(x => x.usage.UsedDateTime)
                .Select(x => new { x.promo.StrPromoCode, x.promo.DecDiscountPercentage })
                .FirstOrDefaultAsync();

            if (promo == null)
                return (null, 0m);

            return (promo.StrPromoCode, promo.DecDiscountPercentage);
        }

        private IActionResult StockConflict(int productId, int available)
        {
            return Conflict(new
            {
                error = "Insufficient stock.",
                productId,
                availableStock = available
            });
        }



    }
}
