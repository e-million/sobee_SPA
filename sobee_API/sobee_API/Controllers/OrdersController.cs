// FILE: sobee_API/sobee_API/Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly SobeecoredbContext _db;

        public OrdersController(SobeecoredbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------------------------------
        // DTOs
        // ---------------------------------------------------------------------

        public sealed class CheckoutRequest
        {
            public string? ShippingAddress { get; set; }
            public int? PaymentMethodId { get; set; }
        }

        private sealed class OrderResponse
        {
            public int OrderId { get; set; }
            public DateTime? OrderDate { get; set; }
            public decimal? TotalAmount { get; set; }
            public string? OrderStatus { get; set; }
            public string OwnerType { get; set; } = "guest";
            public string? UserId { get; set; }
            public string? GuestSessionId { get; set; }
            public List<OrderItemResponse> Items { get; set; } = new();
        }

        private sealed class OrderItemResponse
        {
            public int? OrderItemId { get; set; }
            public int? ProductId { get; set; }
            public string? ProductName { get; set; }
            public decimal? UnitPrice { get; set; }
            public int? Quantity { get; set; }
            public decimal LineTotal { get; set; }
        }

        // ---------------------------------------------------------------------
        // Order status lifecycle helpers
        // ---------------------------------------------------------------------
        private static class OrderStatuses
        {
            // Canonical status strings stored in TOrders.StrOrderStatus
            public const string Pending = "Pending";        // order created
            public const string Paid = "Paid";              // payment captured (if applicable)
            public const string Processing = "Processing";  // being prepared / packed
            public const string Shipped = "Shipped";        // handed to carrier
            public const string Delivered = "Delivered";    // delivered to customer
            public const string Cancelled = "Cancelled";    // cancelled before fulfillment
            public const string Refunded = "Refunded";      // refunded after payment

            private static readonly string[] _all =
            {
                Pending, Paid, Processing, Shipped, Delivered, Cancelled, Refunded
            };

            // Allowed transitions (edit these rules as your business process changes)
            private static readonly Dictionary<string, HashSet<string>> _allowedTransitions =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    [Pending] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Paid, Cancelled },
                    [Paid] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Processing, Refunded },
                    [Processing] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Shipped, Cancelled },
                    [Shipped] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Delivered },
                    [Delivered] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Refunded },
                    [Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { },
                    [Refunded] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { }
                };

            public static IReadOnlyList<string> All => _all;

            public static bool IsKnown(string? status)
                => !string.IsNullOrWhiteSpace(status) && _all.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);

            public static string Normalize(string status)
            {
                status = status.Trim();

                foreach (var s in _all)
                {
                    if (string.Equals(s, status, StringComparison.OrdinalIgnoreCase))
                        return s;
                }

                return status; // caller should reject unknown values via IsKnown
            }

            public static bool CanTransition(string? from, string to)
            {
                from = string.IsNullOrWhiteSpace(from) ? Pending : Normalize(from);
                to = Normalize(to);

                if (!IsKnown(from) || !IsKnown(to))
                    return false;

                if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                    return true; // no-op

                return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
            }

            public static bool IsCancellable(string? status)
            {
                status = string.IsNullOrWhiteSpace(status) ? Pending : Normalize(status);

                // Business rule: cancel allowed before shipment
                return string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, Paid, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase);
            }
        }

        public sealed class UpdateOrderStatusRequest
        {
            public string? Status { get; set; }
        }

        // ---------------------------------------------------------------------
        // ENDPOINTS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Checkout: creates an order from the current cart and clears the cart.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var owner = ResolveOwner();

            if (owner.UserId == null && owner.GuestSessionId == null)
                return Forbid();

            // Load cart for this owner (user OR guest)
            TshoppingCart? cart =
                owner.UserId != null
                    ? await _db.TshoppingCarts
                        .Include(c => c.TcartItems)
                        .ThenInclude(i => i.IntProduct)
                        .FirstOrDefaultAsync(c => c.UserId == owner.UserId)
                    : await _db.TshoppingCarts
                        .Include(c => c.TcartItems)
                        .ThenInclude(i => i.IntProduct)
                        .FirstOrDefaultAsync(c => c.SessionId == owner.GuestSessionId);

            if (cart == null)
                return BadRequest(new { error = "No cart found for this owner." });

            if (cart.TcartItems == null || cart.TcartItems.Count == 0)
                return BadRequest(new { error = "Cart is empty." });

            // Compute totals (also validates items exist)
            decimal total = 0m;

            foreach (var item in cart.TcartItems)
            {
                var qty = item.IntQuantity ?? 0;

                if (qty <= 0)
                    return BadRequest(new { error = "Cart has an item with invalid quantity.", cartItemId = item.IntCartItemId });

                if (item.IntProduct == null)
                    return BadRequest(new { error = "Cart contains an item with missing product reference.", cartItemId = item.IntCartItemId });

                var price = item.IntProduct.DecPrice;
                total += qty * price;
            }

            // Transaction: validate stock + decrement stock + create order + clear cart
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // 1) Validate stock and decrement it (this is what was missing)
                foreach (var cartItem in cart.TcartItems)
                {
                    var qty = cartItem.IntQuantity ?? 0;
                    var product = cartItem.IntProduct!; // validated above

                    if (product.IntStockAmount < qty)
                    {
                        // Not enough stock to fulfill order; rollback later (we haven't saved yet)
                        return Conflict(new
                        {
                            error = "Insufficient stock.",
                            productId = product.IntProductId,
                            productName = product.StrName,
                            requested = qty,
                            available = product.IntStockAmount
                        });
                    }

                    // Decrement inventory now (reservation happens at checkout)
                    product.IntStockAmount -= qty;
                }

                // 2) Create order header
                var order = new Torder
                {
                    DtmOrderDate = DateTime.UtcNow,
                    DecTotalAmount = total,
                    StrShippingAddress = request.ShippingAddress,
                    IntPaymentMethodId = request.PaymentMethodId,
                    StrOrderStatus = OrderStatuses.Pending, // lifecycle starts here
                    UserId = owner.UserId,
                    SessionId = owner.GuestSessionId
                };

                _db.Torders.Add(order);
                await _db.SaveChangesAsync(); // gets order.IntOrderId

                // 3) Create order items
                foreach (var cartItem in cart.TcartItems)
                {
                    var qty = cartItem.IntQuantity ?? 0;
                    var price = cartItem.IntProduct?.DecPrice ?? 0m;

                    var orderItem = new TorderItem
                    {
                        IntOrderId = order.IntOrderId,
                        IntProductId = cartItem.IntProductId,
                        IntQuantity = qty,
                        MonPricePerUnit = price
                    };

                    _db.TorderItems.Add(orderItem);
                }

                // 4) Clear cart items
                _db.TcartItems.RemoveRange(cart.TcartItems);
                cart.DtmDateLastUpdated = DateTime.UtcNow;

                // 5) Persist everything: (stock changes + order + items + cart clear)
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Reload order with items for response
                var created = await _db.Torders
                    .Include(o => o.TorderItems)
                    .ThenInclude(oi => oi.IntProduct)
                    .FirstAsync(o => o.IntOrderId == order.IntOrderId);

                return Ok(ToOrderResponse(created));
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = "Checkout failed." });
            }
        }

        /// <summary>
        /// Get a single order (owner-only: user or guest session).
        /// </summary>
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var owner = ResolveOwner();

            if (owner.UserId == null && owner.GuestSessionId == null)
                return Forbid();

            var query = _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .AsQueryable();

            if (owner.UserId != null)
                query = query.Where(o => o.UserId == owner.UserId);
            else
                query = query.Where(o => o.SessionId == owner.GuestSessionId);

            var order = await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);

            if (order == null)
                return NotFound(new { error = "Order not found." });

            return Ok(ToOrderResponse(order));
        }

        /// <summary>
        /// Authenticated: get my orders.
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var orders = await _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.DtmOrderDate)
                .Select(o => ToOrderResponse(o))
                .ToListAsync();

            return Ok(orders);
        }

        // ---------------------------------------------------------------------
        // Order status lifecycle endpoints
        // ---------------------------------------------------------------------

        /// <summary>
        /// Admin-only: update an order's status with transition validation.
        /// </summary>
        [HttpPatch("{orderId:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            if (request?.Status == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { error = "Missing required field: status" });

            var newStatus = OrderStatuses.Normalize(request.Status);

            if (!OrderStatuses.IsKnown(newStatus))
                return BadRequest(new { error = "Invalid status value.", allowed = OrderStatuses.All });

            var order = await _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .FirstOrDefaultAsync(o => o.IntOrderId == orderId);

            if (order == null)
                return NotFound(new { error = "Order not found." });

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.CanTransition(currentStatus, newStatus))
            {
                return Conflict(new
                {
                    error = "Invalid status transition.",
                    from = currentStatus,
                    to = newStatus,
                    allowedNext = OrderStatuses.All.Where(s => OrderStatuses.CanTransition(currentStatus, s)).ToArray()
                });
            }

            // Status change only. Stock already decremented at checkout.
            order.StrOrderStatus = newStatus;
            await _db.SaveChangesAsync();

            return Ok(ToOrderResponse(order));
        }

        /// <summary>
        /// Owner (authenticated user or guest with valid session): cancel an order when allowed.
        /// </summary>
        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var owner = ResolveOwner();

            if (owner.UserId == null && owner.GuestSessionId == null)
                return Forbid();

            var query = _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .AsQueryable();

            if (owner.UserId != null)
                query = query.Where(o => o.UserId == owner.UserId);
            else
                query = query.Where(o => o.SessionId == owner.GuestSessionId);

            var order = await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);

            if (order == null)
                return NotFound(new { error = "Order not found." });

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.IsCancellable(currentStatus))
            {
                return Conflict(new
                {
                    error = "Order cannot be cancelled in its current status.",
                    status = currentStatus
                });
            }

            // Optional: restock items here if you want "cancel" to return stock.
            order.StrOrderStatus = OrderStatuses.Cancelled;

            await _db.SaveChangesAsync();

            return Ok(ToOrderResponse(order));
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private (string? UserId, string? GuestSessionId, string OwnerType) ResolveOwner()
        {
            // Authenticated user
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                    return (null, null, "invalid");

                return (userId, null, "user");
            }

            // Guest session
            if (Request.Headers.TryGetValue("X-Session-Id", out var values))
            {
                var sessionId = values.ToString();

                if (!string.IsNullOrWhiteSpace(sessionId))
                    return (null, sessionId, "guest");
            }

            return (null, null, "none");
        }

        private static OrderResponse ToOrderResponse(Torder order)
        {
            var ownerType = !string.IsNullOrWhiteSpace(order.UserId) ? "user" : "guest";

            var resp = new OrderResponse
            {
                OrderId = order.IntOrderId,
                OrderDate = order.DtmOrderDate,
                TotalAmount = order.DecTotalAmount,
                OrderStatus = order.StrOrderStatus,
                OwnerType = ownerType,
                UserId = order.UserId,
                GuestSessionId = order.SessionId
            };

            if (order.TorderItems != null)
            {
                foreach (var item in order.TorderItems)
                {
                    var qty = item.IntQuantity ?? 0;
                    var unit = item.MonPricePerUnit ?? (item.IntProduct?.DecPrice ?? 0m);

                    resp.Items.Add(new OrderItemResponse
                    {
                        OrderItemId = item.IntOrderItemId,
                        ProductId = item.IntProductId,
                        ProductName = item.IntProduct?.StrName,
                        UnitPrice = unit,
                        Quantity = qty,
                        LineTotal = qty * unit
                    });
                }
            }

            return resp;
        }
    }
}
