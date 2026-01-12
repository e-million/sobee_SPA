// FILE: sobee_API/sobee_API/Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Cart;
using Sobee.Domain.Entities.Orders;
using sobee_API.Constants;
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
                return NotFoundError("Order not found.", "NotFound", new { orderId });

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
                    return BadRequestError(
                        "Cart has an item with invalid quantity.",
                        "ValidationError",
                        new { cartItemId = item.IntCartItemId });

                if (item.IntProduct == null)
                    return BadRequestError(
                        "Cart contains an item with missing product reference.",
                        "ValidationError",
                        new { cartItemId = item.IntCartItemId });

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
                        return ConflictError(
                            "Insufficient stock.",
                            "InsufficientStock",
                            new
                            {
                                productId = product.IntProductId,
                                productName = product.StrName,
                                requested = qty,
                                availableStock = product.IntStockAmount
                            });
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
                return NotFoundError("Order not found.", "NotFound", new { orderId });

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.IsCancellable(currentStatus))
            {
                return ConflictError(
                    "Order cannot be cancelled in its current status.",
                    "InvalidStatusTransition",
                    new { status = currentStatus });
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
                return NotFoundError("Order not found.", "NotFound", new { orderId });

            var paymentMethod = await _db.TpaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.IntPaymentMethodId == request.PaymentMethodId);

            if (paymentMethod == null)
                return NotFoundError(
                    $"Payment method {request.PaymentMethodId} not found.",
                    "NotFound",
                    new { paymentMethodId = request.PaymentMethodId }
                );

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            var targetStatus = OrderStatuses.Paid;

            if (!OrderStatuses.CanTransition(currentStatus, targetStatus))
            {
                return ConflictError(
                    "Invalid status transition.",
                    "InvalidStatusTransition",
                    new
                    {
                        orderId,
                        fromStatus = order.StrOrderStatus,
                        toStatus = targetStatus
                    }
                );
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
            var newStatus = OrderStatuses.Normalize(request.Status!);

            var order = await _db.Torders
                .Include(o => o.TorderItems)
                .ThenInclude(oi => oi.IntProduct)
                .FirstOrDefaultAsync(o => o.IntOrderId == orderId);

            if (order == null)
                return NotFoundError("Order not found.", "NotFound", new { orderId });

            var currentStatus = string.IsNullOrWhiteSpace(order.StrOrderStatus)
                ? OrderStatuses.Pending
                : OrderStatuses.Normalize(order.StrOrderStatus);

            if (!OrderStatuses.CanTransition(currentStatus, newStatus))
            {
                return ConflictError(
                    "Invalid status transition.",
                    "InvalidStatusTransition",
                    new
                    {
                        orderId,
                        fromStatus = order.StrOrderStatus,
                        toStatus = request.Status
                    }
                );
            }

            // Status change only. Stock already decremented at checkout.
            order.StrOrderStatus = newStatus;
            await _db.SaveChangesAsync();

            return Ok(ToOrderResponse(order));
        }


        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

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

        private BadRequestObjectResult BadRequestError(string message, string? code = null, object? details = null)
            => BadRequest(new ApiErrorResponse(message, code, details));

        private NotFoundObjectResult NotFoundError(string message, string? code = null, object? details = null)
            => NotFound(new ApiErrorResponse(message, code, details));

        private ConflictObjectResult ConflictError(string message, string? code = null, object? details = null)
            => Conflict(new ApiErrorResponse(message, code, details));

        private UnauthorizedObjectResult UnauthorizedError(string message, string? code = null, object? details = null)
            => Unauthorized(new ApiErrorResponse(message, code, details));

        private ObjectResult ForbiddenError(string message, string? code = null, object? details = null)
            => StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(message, code, details));

        private ObjectResult ServerError(string message = "An unexpected error occurred.", string? code = "ServerError", object? details = null)
            => StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse(message, code, details));


    }
}
