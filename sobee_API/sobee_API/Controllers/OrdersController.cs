// FILE: sobee_API/sobee_API/Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using sobee_API.DTOs.Common;
using sobee_API.DTOs.Orders;
using sobee_API.Services;
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
        private readonly RequestIdentityResolver _identityResolver;

        public OrdersController(SobeecoredbContext db, RequestIdentityResolver identityResolver)
        {
            _db = db;
            _identityResolver = identityResolver;
        }


        // ---------------------------------------------------------------------
        // ENDPOINTS
        // ---------------------------------------------------------------------

        /// <summary>
        /// Get a single order (owner-only: user or guest session).
        /// </summary>
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: false);
            if (errorResult != null)
                return errorResult;

            var query = _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .AsQueryable();

            if (identity!.UserId != null)
                query = query.Where(o => o.UserId == identity.UserId);
            else
                query = query.Where(o => o.SessionId == identity.GuestSessionId);

            var order = await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);
            if (order == null)
                return NotFoundError("Order not found.", "NotFound");

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
                return UnauthorizedError("Missing NameIdentifier claim.", "Unauthorized");

            var orders = await _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.DtmOrderDate)
                .Select(o => ToOrderResponse(o))
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Checkout: creates an order from the current cart and clears the cart.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (request == null)
                return BadRequestError("Request body is required.", "ValidationError");

            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                return BadRequestError("ShippingAddress is required.", "ValidationError");

            // Require an existing validated guest session if not authenticated
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: false);
            if (errorResult != null)
                return errorResult;

            // Load cart for this owner (user OR guest)
            TshoppingCart? cart =
                identity!.UserId != null
                    ? await _db.TshoppingCarts
                        .Include(c => c.TcartItems)
                        .ThenInclude(i => i.IntProduct)
                        .FirstOrDefaultAsync(c => c.UserId == identity.UserId)
                    : await _db.TshoppingCarts
                        .Include(c => c.TcartItems)
                        .ThenInclude(i => i.IntProduct)
                        .FirstOrDefaultAsync(c => c.SessionId == identity.GuestSessionId);

            if (cart == null)
                return BadRequestError("No cart found for this owner.", "ValidationError");

            if (cart.TcartItems == null || cart.TcartItems.Count == 0)
                return BadRequestError("Cart is empty.", "ValidationError");

            // Compute totals (also validates items exist)
            decimal subtotal = 0m;

            foreach (var item in cart.TcartItems)
            {
                var qty = item.IntQuantity ?? 0;

                if (qty <= 0)
                    return BadRequestError("Cart has an item with invalid quantity.", "ValidationError");

                if (item.IntProduct == null)
                    return BadRequestError("Cart contains an item with missing product reference.", "ValidationError");

                subtotal += qty * item.IntProduct.DecPrice;
            }

            // Promo discount (cart-scoped)
            decimal discount = 0m;

            var promo = await _db.TpromoCodeUsageHistories
                .Join(_db.Tpromotions,
                    usage => usage.PromoCode,
                    promo => promo.StrPromoCode,
                    (usage, promo) => new { usage, promo })
                .Where(x => x.usage.IntShoppingCartId == cart.IntShoppingCartId &&
                            x.promo.DtmExpirationDate > DateTime.UtcNow)
                .OrderByDescending(x => x.usage.UsedDateTime)
                .Select(x => x.promo)
                .FirstOrDefaultAsync();

            if (promo != null)
            {
                discount = subtotal * (promo.DecDiscountPercentage / 100m);
                if (discount < 0) discount = 0;
                if (discount > subtotal) discount = subtotal;
            }

            decimal total = subtotal - discount;
            if (total < 0) total = 0;

            // Transaction: validate stock + decrement stock + create order + clear cart
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // 1) Validate stock and decrement it
                foreach (var cartItem in cart.TcartItems)
                {
                    var qty = cartItem.IntQuantity ?? 0;
                    var product = cartItem.IntProduct!;

                    if (product.IntStockAmount < qty)
                    {
                        return ConflictError("Insufficient stock.", "Conflict");
                    }

                    product.IntStockAmount -= qty;
                }

                // 2) Create order header
                var order = new Torder
                {
                    DtmOrderDate = DateTime.UtcNow,

                    // pricing snapshot
                    DecSubtotalAmount = subtotal,
                    DecDiscountPercentage = promo?.DecDiscountPercentage,
                    DecDiscountAmount = discount,
                    StrPromoCode = promo?.StrPromoCode,
                    DecTotalAmount = total,

                    StrShippingAddress = request.ShippingAddress,
                    IntPaymentMethodId = request.PaymentMethodId,
                    StrOrderStatus = OrderStatuses.Pending,
                    UserId = identity.UserId,
                    SessionId = identity.GuestSessionId
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

                // 4.5) Clear applied promo(s) for this cart (since order has the snapshot)
                var appliedPromos = await _db.TpromoCodeUsageHistories
                    .Where(p => p.IntShoppingCartId == cart.IntShoppingCartId)
                    .ToListAsync();

                if (appliedPromos.Count > 0)
                {
                    _db.TpromoCodeUsageHistories.RemoveRange(appliedPromos);
                }

                // 5) Persist everything: (stock changes + order + items + cart clear + promo clear)
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
                return ServerError("Checkout failed.");
            }
        }

        /// <summary>
        /// Owner (authenticated user or guest with valid session): cancel an order when allowed.
        /// </summary>
        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: false);
            if (errorResult != null)
                return errorResult;

            var query = _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .AsQueryable();

            if (identity!.UserId != null)
                query = query.Where(o => o.UserId == identity.UserId);
            else
                query = query.Where(o => o.SessionId == identity.GuestSessionId);

            var order = await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);
            if (order == null)
                return NotFoundError("Order not found.", "NotFound");

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.IsCancellable(currentStatus))
            {
                return ConflictError("Order cannot be cancelled in its current status.", "InvalidStatusTransition");
            }

            order.StrOrderStatus = OrderStatuses.Cancelled;
            await _db.SaveChangesAsync();

            return Ok(ToOrderResponse(order));
        }


        /// <summary>
        /// Owner-only (authenticated user or guest session): marks an order as Paid (placeholder payment).
        /// Creates a TPayment row and updates the order's status.
        /// </summary>
        [HttpPost("{orderId:int}/pay")]
        public async Task<IActionResult> PayOrder(int orderId, [FromBody] PayOrderRequest request)
        {
            if (request == null)
                return BadRequestError("Request body is required.", "ValidationError");

            if (request.PaymentMethodId <= 0)
                return BadRequestError("PaymentMethodId must be a positive integer.", "ValidationError");

            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: false);
            if (errorResult != null)
                return errorResult;

            var query = _db.Torders.AsQueryable();

            if (identity!.UserId != null)
                query = query.Where(o => o.UserId == identity.UserId);
            else
                query = query.Where(o => o.SessionId == identity.GuestSessionId);

            var order = await query.FirstOrDefaultAsync(o => o.IntOrderId == orderId);
            if (order == null)
                return NotFoundError("Order not found.", "NotFound");

            var paymentMethod = await _db.TpaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.IntPaymentMethodId == request.PaymentMethodId);

            if (paymentMethod == null)
                return NotFoundError($"Payment method {request.PaymentMethodId} not found.", "NotFound");

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            var targetStatus = OrderStatuses.Paid;

            if (!OrderStatuses.CanTransition(currentStatus, targetStatus))
            {
                return ConflictError("Invalid status transition.", "InvalidStatusTransition");
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var payment = new Sobee.Domain.Entities.Payments.Tpayment
                {
                    IntPaymentMethodId = paymentMethod.IntPaymentMethodId,
                    StrBillingAddress = paymentMethod.StrBillingAddress
                };

                _db.Tpayments.Add(payment);

                order.IntPaymentMethodId = paymentMethod.IntPaymentMethodId;
                order.StrOrderStatus = targetStatus;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                var updated = await _db.Torders
                    .Include(o => o.TorderItems)
                    .ThenInclude(oi => oi.IntProduct)
                    .FirstAsync(o => o.IntOrderId == order.IntOrderId);

                return Ok(ToOrderResponse(updated));
            }
            catch
            {
                await tx.RollbackAsync();
                return ServerError("Payment failed.");
            }
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
                return BadRequestError("Missing required field: status", "ValidationError");

            var newStatus = OrderStatuses.Normalize(request.Status);

            if (!OrderStatuses.IsKnown(newStatus))
                return BadRequestError("Invalid status value.", "ValidationError");

            var order = await _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .FirstOrDefaultAsync(o => o.IntOrderId == orderId);

            if (order == null)
                return NotFoundError("Order not found.", "NotFound");

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.CanTransition(currentStatus, newStatus))
            {
                return ConflictError("Invalid status transition.", "InvalidStatusTransition");
            }

            // Status change only. Stock already decremented at checkout.
            order.StrOrderStatus = newStatus;
            await _db.SaveChangesAsync();

            return Ok(ToOrderResponse(order));
        }


        // ---------------------------------------------------------------------
        // Helpers
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

        private async Task<(RequestIdentity? identity, IActionResult? errorResult)> ResolveIdentityAsync(bool allowCreateGuestSession)
        {
            var identity = await _identityResolver.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession,
                allowAuthenticatedGuestSession: false);

            if (identity.HasError)
            {
                if (identity.ErrorCode == "MissingNameIdentifier")
                    return (null, UnauthorizedError(identity.ErrorMessage, "Unauthorized"));

                return (null, BadRequestError(identity.ErrorMessage, "ValidationError"));
            }

            // For anonymous requests we require validated guest session (unless allowCreateGuestSession=true)
            if (!identity.IsAuthenticated && !identity.GuestSessionValidated)
            {
                return (null, BadRequestError("Missing or invalid guest session headers.", "Unauthorized"));
            }

            return (identity, null);
        }


        //private (string? UserId, string? GuestSessionId, string OwnerType) ResolveOwner()
        //{
        //    // Authenticated user
        //    if (User?.Identity?.IsAuthenticated == true)
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //        if (string.IsNullOrWhiteSpace(userId))
        //            return (null, null, "invalid");

        //        return (userId, null, "user");
        //    }

        //    // Guest session
        //    if (Request.Headers.TryGetValue("X-Session-Id", out var values))
        //    {
        //        var sessionId = values.ToString();

        //        if (!string.IsNullOrWhiteSpace(sessionId))
        //            return (null, sessionId, "guest");
        //    }

        //    return (null, null, "none");
        //}

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
                GuestSessionId = order.SessionId,
                SubtotalAmount = order.DecSubtotalAmount,
                DiscountAmount = order.DecDiscountAmount,
                DiscountPercentage = order.DecDiscountPercentage,
                PromoCode = order.StrPromoCode,

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

        private BadRequestObjectResult BadRequestError(string message, string? code = null)
            => BadRequest(new ApiErrorResponse(message, code));

        private NotFoundObjectResult NotFoundError(string message, string? code = null)
            => NotFound(new ApiErrorResponse(message, code));

        private ConflictObjectResult ConflictError(string message, string? code = null)
            => Conflict(new ApiErrorResponse(message, code));

        private UnauthorizedObjectResult UnauthorizedError(string message, string? code = null)
            => Unauthorized(new ApiErrorResponse(message, code));

        private ObjectResult ForbiddenError(string message, string? code = null)
            => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(message, code));

        private ObjectResult ServerError(string message = "An unexpected error occurred.", string? code = "ServerError")
            => StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(message, code));


    }
}
