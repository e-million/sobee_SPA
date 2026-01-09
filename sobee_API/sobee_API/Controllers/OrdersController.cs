using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private const string SessionHeaderName = "X-Session-Id";
        private readonly SobeecoredbContext _db;

        public OrdersController(SobeecoredbContext db)
        {
            _db = db;
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
        // - Guest: requires X-Session-Id
        // - If authenticated + X-Session-Id: merges guest cart -> user cart first
        // ============================================================
        [HttpPost("checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ShippingAddress))
                return BadRequest(new { error = "ShippingAddress is required." });

            var (userId, sessionId, ownerType) = ResolveOwner();

            // Guest must provide X-Session-Id for checkout
            if (ownerType == "guest" && string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { error = $"Guest checkout requires '{SessionHeaderName}' header." });

            // If logged in and a session header exists, merge guest cart into user cart
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                await MergeGuestCartIntoUserCartAsync(userId, sessionId);
                await MigrateGuestOrdersToUserAsync(userId, sessionId);
            }

            // Load cart (after merge, user cart should exist if anything was in session cart)
            var cart = await LoadCartAsync(userId, sessionId);
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

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
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
                return StatusCode(500, new { error = "Checkout failed.", details = ex.Message });
            }
        }

        // ============================================================
        // GET: /api/orders/{orderId}
        // - User: must own (UserId matches)
        // - Guest: must own (SessionId matches X-Session-Id)
        // ============================================================
        [HttpGet("{orderId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await LoadOrderAsync(orderId);
            if (order == null)
                return NotFound(new { error = "Order not found." });

            var (userId, sessionId, ownerType) = ResolveOwner();

            if (ownerType == "user")
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { error = "Missing user id claim." });

                if (!string.Equals(order.UserId, userId, StringComparison.Ordinal))
                    return Forbid();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                    return BadRequest(new { error = $"Guest requests require '{SessionHeaderName}' header." });

                if (!string.Equals(order.SessionId, sessionId, StringComparison.Ordinal))
                    return Forbid();
            }

            return Ok(ToOrderResponse(order, ownerType));
        }

        // ============================================================
        // GET: /api/orders/my
        // - Authenticated only
        // - If X-Session-Id exists: migrate guest orders to user first
        // ============================================================
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "Authenticated request is missing NameIdentifier claim." });

            // If client still has an old guest session id, migrate those orders into the user
            string? sessionId = GetSessionIdFromHeader();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await MigrateGuestOrdersToUserAsync(userId, sessionId);
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

        private (string? userId, string? sessionId, string ownerType) ResolveOwner()
        {
            var sessionId = GetSessionIdFromHeader();

            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return (userId, sessionId, "user");
            }

            return (null, sessionId, "guest");
        }

        private string? GetSessionIdFromHeader()
        {
            if (Request.Headers.TryGetValue(SessionHeaderName, out var sidValues))
            {
                var sid = sidValues.ToString();
                if (!string.IsNullOrWhiteSpace(sid))
                    return sid.Trim();
            }

            return null;
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
    }
}
