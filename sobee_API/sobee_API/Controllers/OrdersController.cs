using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using sobee_API.Services;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly SobeecoredbContext _db;
        private readonly GuestSessionService _guestSessionService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(SobeecoredbContext db, GuestSessionService guestSessionService, ILogger<OrdersController> logger)
        {
            _db = db;
            _guestSessionService = guestSessionService;
            _logger = logger;
        }

        // ============================================================
        // DTOs
        // ============================================================
        public class CheckoutRequest
        {
            public string ShippingAddress { get; set; } = string.Empty;
            public int? ShippingMethodId { get; set; }
            public int? PaymentMethodId { get; set; }
        }

        public class OrderItemResponse
        {
            public int OrderItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal PricePerUnit { get; set; }
            public int Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }

        public class OrderResponse
        {
            public int OrderId { get; set; }
            public string OwnerType { get; set; } = string.Empty; // "user" or "guest"
            public string? UserId { get; set; }
            public string? SessionId { get; set; }

            public DateTime? OrderDateUtc { get; set; }
            public string? OrderStatus { get; set; }
            public decimal? TotalAmount { get; set; }

            public string? ShippingAddress { get; set; }
            public int? ShippingStatusId { get; set; }

            public List<OrderItemResponse> Items { get; set; } = new();
        }

        // ============================================================
        // POST: /api/orders/checkout
        // - Authenticated: user checkout
        // - Guest: requires X-Session-Id + X-Session-Secret
        // - If authenticated + validated guest session: merges guest cart -> user cart first
        // ============================================================
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ShippingAddress))
                return BadRequest(new { error = "ShippingAddress is required." });

            var (owner, errorResult) = await ResolveOwnerAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var userId = owner!.UserId;
            var sessionId = owner.SessionId;
            var ownerType = owner.OwnerType;

            if (ownerType == "guest" && string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { error = $"Guest checkout requires '{GuestSessionService.SessionIdHeaderName}' and '{GuestSessionService.SessionSecretHeaderName}' headers." });

            await using var tx = await _db.Database.BeginTransactionAsync();
            TshoppingCart? cart = null;

            try
            {
                // If logged in and a validated guest session exists, merge/migrate within this transaction.
                if (!string.IsNullOrWhiteSpace(userId) && owner.GuestSessionValidated && !string.IsNullOrWhiteSpace(sessionId))
                {
                    await MergeGuestCartIntoUserCartAsync(userId, sessionId);
                    await MigrateGuestOrdersToUserAsync(userId, sessionId);
                    await RotateGuestSessionAsync(sessionId);
                }

                // Load cart (after merge, user cart should exist if anything was in session cart)
                cart = await LoadCartAsync(userId, sessionId);
                if (cart == null)
                    return NotFound(new { error = "Cart not found." });

                if (cart.TcartItems == null || cart.TcartItems.Count == 0)
                    return BadRequest(new { error = "Cart is empty." });

                foreach (var ci in cart.TcartItems)
                {
                    if (ci.IntProductId == null || ci.IntProduct == null)
                        return BadRequest(new { error = "Cart contains invalid item(s). A product is missing." });

                    if ((ci.IntQuantity ?? 0) <= 0)
                        return BadRequest(new { error = "Cart contains invalid quantity." });
                }

                int? pendingShippingStatusId = await TryGetPendingShippingStatusIdAsync();

                // Create order header
                var order = new Torder
                {
                    DtmOrderDate = DateTime.UtcNow,
                    StrOrderStatus = "Pending",
                    StrShippingAddress = request.ShippingAddress,
                    IntShippingStatusId = pendingShippingStatusId,

                    // Ownership
                    UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                    SessionId = string.IsNullOrWhiteSpace(userId) ? sessionId : null
                };

                _db.Torders.Add(order);
                await _db.SaveChangesAsync(); // get IntOrderId

                // Create order items from cart items
                var orderItems = new List<TorderItem>();
                decimal total = 0m;

                foreach (var cartItem in cart.TcartItems)
                {
                    int qty = cartItem.IntQuantity ?? 0;
                    var product = cartItem.IntProduct!;
                    decimal price = product.DecPrice;

                    var oi = new TorderItem
                    {
                        IntOrderId = order.IntOrderId,
                        IntProductId = product.IntProductId,
                        IntQuantity = qty,
                        MonPricePerUnit = price
                    };

                    orderItems.Add(oi);
                    total += price * qty;
                }

                _db.TorderItems.AddRange(orderItems);

                // Set order total
                order.DecTotalAmount = total;

                // Clear cart items (cart row remains)
                _db.TcartItems.RemoveRange(cart.TcartItems);

                cart.DtmDateLastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var created = await LoadOrderAsync(order.IntOrderId);
                return Ok(ToOrderResponse(created!, ownerType: string.IsNullOrWhiteSpace(userId) ? "guest" : "user"));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Checkout failed for user {UserId} cart {CartId} session {SessionId}.", userId, cart?.IntShoppingCartId, sessionId);
                return StatusCode(500, new { error = "Checkout failed." });
            }
        }

        // ============================================================
        // GET: /api/orders/{orderId}
        // - User: must own (UserId matches)
        // - Guest: must own (SessionId matches validated guest session)
        // ============================================================
        [HttpGet("{orderId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await LoadOrderAsync(orderId);
            if (order == null)
                return NotFound(new { error = "Order not found." });

            var (owner, errorResult) = await ResolveOwnerAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            if (owner!.OwnerType == "user")
            {
                if (!string.Equals(order.UserId, owner.UserId, StringComparison.Ordinal))
                    return Forbid();
            }
            else
            {
                if (!owner.GuestSessionValidated)
                    return StatusCode(403, new { error = "Guest session is invalid." });

                if (!string.Equals(order.SessionId, owner.SessionId, StringComparison.Ordinal))
                    return Forbid();
            }

            return Ok(ToOrderResponse(order, owner.OwnerType));
        }

        // ============================================================
        // GET: /api/orders/my
        // - Authenticated only
        // - If validated guest session exists: migrate guest orders to user first
        // ============================================================
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "Authenticated request is missing NameIdentifier claim." });

            var guestSession = await _guestSessionService.ResolveAsync(Request, Response, allowCreate: false);
            if (guestSession.WasValidated && !string.IsNullOrWhiteSpace(guestSession.SessionId))
            {
                await MigrateGuestOrdersToUserAsync(userId, guestSession.SessionId);
                await RotateGuestSessionAsync(guestSession.SessionId);
            }

            var orders = await _db.Torders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.DtmOrderDate)
                .Include(o => o.TorderItems)
                    .ThenInclude(oi => oi.IntProduct)
                .ToListAsync();

            var res = orders.Select(o => ToOrderResponse(o, "user")).ToList();
            return Ok(res);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private async Task<(OrderOwner? owner, IActionResult? errorResult)> ResolveOwnerAsync()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return (null, Unauthorized(new { error = "Authenticated request is missing NameIdentifier claim." }));
                }

                var guestSession = await _guestSessionService.ResolveAsync(Request, Response, allowCreate: false);
                return (new OrderOwner(userId, guestSession.WasValidated ? guestSession.SessionId : null, "user", guestSession.WasValidated), null);
            }

            var guestSessionForAnonymous = await _guestSessionService.ResolveAsync(Request, Response, allowCreate: true);
            return (new OrderOwner(null, guestSessionForAnonymous.SessionId, "guest", guestSessionForAnonymous.WasValidated), null);
        }

        private async Task<TshoppingCart?> LoadCartAsync(string? userId, string? sessionId)
        {
            var query = _db.TshoppingCarts
                .Include(c => c.TcartItems)
                    .ThenInclude(i => i.IntProduct)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
                return await query.FirstOrDefaultAsync(c => c.UserId == userId);

            if (!string.IsNullOrWhiteSpace(sessionId))
                return await query.FirstOrDefaultAsync(c => c.SessionId == sessionId);

            return null;
        }

        private async Task<Torder?> LoadOrderAsync(int orderId)
        {
            return await _db.Torders
                .Include(o => o.TorderItems)
                    .ThenInclude(oi => oi.IntProduct)
                .FirstOrDefaultAsync(o => o.IntOrderId == orderId);
        }

        private OrderResponse ToOrderResponse(Torder order, string ownerType)
        {
            var response = new OrderResponse
            {
                OrderId = order.IntOrderId,
                OwnerType = ownerType,
                UserId = order.UserId,
                SessionId = order.SessionId,
                OrderDateUtc = order.DtmOrderDate,
                OrderStatus = order.StrOrderStatus,
                TotalAmount = order.DecTotalAmount,
                ShippingAddress = order.StrShippingAddress,
                ShippingStatusId = order.IntShippingStatusId
            };

            foreach (var item in order.TorderItems)
            {
                var product = item.IntProduct;

                int qty = item.IntQuantity ?? 0;
                decimal price = item.MonPricePerUnit ?? 0m;

                response.Items.Add(new OrderItemResponse
                {
                    OrderItemId = item.IntOrderItemId,
                    ProductId = item.IntProductId ?? 0,
                    ProductName = product?.StrName ?? "(unknown)",
                    PricePerUnit = price,
                    Quantity = qty,
                    LineTotal = price * qty
                });
            }

            return response;
        }

        private async Task<int?> TryGetPendingShippingStatusIdAsync()
        {
            // If you have a "Pending" row in TshippingStatuses and want to use it:
            // return await _db.TshippingStatuses
            //     .Where(s => s.StrStatusName == "Pending")
            //     .Select(s => (int?)s.IntShippingStatusId)
            //     .FirstOrDefaultAsync();

            return null; // MVP: allow null
        }

        /// <summary>
        /// If user is authenticated and still has a guest session cart, merge it into user cart.
        /// </summary>
        private async Task MergeGuestCartIntoUserCartAsync(string userId, string sessionId)
        {
            var userCart = await _db.TshoppingCarts
                .Include(c => c.TcartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var sessionCart = await _db.TshoppingCarts
                .Include(c => c.TcartItems)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);

            if (sessionCart == null)
                return;

            if (userCart == null)
            {
                // Claim session cart as the user's cart
                sessionCart.UserId = userId;
                sessionCart.SessionId = null;
                sessionCart.DtmDateLastUpdated = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return;
            }

            // Merge items
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

            // Remove guest cart after merge
            _db.TshoppingCarts.Remove(sessionCart);

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Migrates guest orders (SessionId, UserId null) to user after login.
        /// </summary>
        private async Task MigrateGuestOrdersToUserAsync(string userId, string sessionId)
        {
            var guestOrders = await _db.Torders
                .Where(o => o.SessionId == sessionId && o.UserId == null)
                .ToListAsync();

            if (guestOrders.Count == 0)
                return;

            foreach (var o in guestOrders)
            {
                o.UserId = userId;
                o.SessionId = null;
            }

            await _db.SaveChangesAsync();
        }

        private async Task RotateGuestSessionAsync(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            // Rotate to prevent guest session fixation reuse after merge/migrate.
            await _guestSessionService.InvalidateAsync(sessionId);
        }

        private record OrderOwner(string? UserId, string? SessionId, string OwnerType, bool GuestSessionValidated);
    }
}
