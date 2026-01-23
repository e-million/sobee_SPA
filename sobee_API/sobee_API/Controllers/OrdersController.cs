// FILE: sobee_API/sobee_API/Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Constants;
using sobee_API.Domain;
using sobee_API.DTOs.Orders;
using sobee_API.Services;
using sobee_API.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;


namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ApiControllerBase
    {
        private readonly RequestIdentityResolver _identityResolver;
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService, RequestIdentityResolver identityResolver)
        {
            _orderService = orderService;
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

            var result = await _orderService.GetOrderAsync(identity!.UserId, identity.GuestSessionId, orderId);
            return FromServiceResult(result);
        }


        /// <summary>
        /// Authenticated: get my orders.
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return UnauthorizedError("Missing NameIdentifier claim.", "Unauthorized");

            var result = await _orderService.GetUserOrdersAsync(userId, page, pageSize);
            if (!result.Success)
            {
                return FromServiceResult(result);
            }

            Response.Headers["X-Total-Count"] = result.Value.TotalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(result.Value.Orders);
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

            var result = await _orderService.CheckoutAsync(identity!.UserId, identity.GuestSessionId, request);
            return FromServiceResult(result);
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

            var result = await _orderService.CancelOrderAsync(identity!.UserId, identity.GuestSessionId, orderId);
            return FromServiceResult(result);
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

            var result = await _orderService.PayOrderAsync(identity!.UserId, identity.GuestSessionId, orderId, request);
            return FromServiceResult(result);
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
            var result = await _orderService.UpdateStatusAsync(orderId, request);
            return FromServiceResult(result);
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

        private IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Value);
            }

            var code = result.ErrorCode ?? "ServerError";
            var message = result.ErrorMessage ?? "An unexpected error occurred.";

            return code switch
            {
                "NotFound" => NotFoundError(message, code, result.ErrorData),
                "ValidationError" => BadRequestError(message, code, result.ErrorData),
                "Unauthorized" => UnauthorizedError(message, code, result.ErrorData),
                "Forbidden" => ForbiddenError(message, code, result.ErrorData),
                "Conflict" => ConflictError(message, code, result.ErrorData),
                "InsufficientStock" => ConflictError(message, code, result.ErrorData),
                "InvalidStatusTransition" => ConflictError(message, code, result.ErrorData),
                _ => ServerError(message, code, result.ErrorData)
            };
        }

    }
}
