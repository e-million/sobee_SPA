using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs;
using sobee_API.DTOs.Cart;
using sobee_API.Services;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ApiControllerBase
    {
        private readonly RequestIdentityResolver _identityResolver;
        private readonly ICartService _cartService;

        public CartController(
            ICartService cartService,
            RequestIdentityResolver identityResolver)
        {
            _identityResolver = identityResolver;
            _cartService = cartService;
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

            var result = await _cartService.GetCartAsync(
                identity!.UserId,
                identity.GuestSessionId,
                identity.GuestSessionValidated);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Add an item to the cart (increments quantity if already present).
        /// </summary>
        [HttpPost("items")]
        [AllowAnonymous]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
                return errorResult;

            var result = await _cartService.AddItemAsync(
                identity!.UserId,
                identity.GuestSessionId,
                identity.GuestSessionValidated,
                request);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Apply a promo code to the current cart.
        /// </summary>
        [HttpPost("promo/apply")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
                return errorResult;

            var result = await _cartService.ApplyPromoAsync(
                identity!.UserId,
                identity.GuestSessionId,
                request.PromoCode);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Update a cart item's quantity (0 removes the item).
        /// </summary>
        [HttpPut("items/{cartItemId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            var (identity, errorResult) = await ResolveIdentityAsync(allowCreateGuestSession: true);
            if (errorResult != null)
                return errorResult;

            var result = await _cartService.UpdateItemAsync(
                identity!.UserId,
                identity.GuestSessionId,
                cartItemId,
                request);
            return FromServiceResult(result);
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

            var result = await _cartService.RemoveItemAsync(
                identity!.UserId,
                identity.GuestSessionId,
                cartItemId);
            return FromServiceResult(result);
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

            var result = await _cartService.ClearCartAsync(
                identity!.UserId,
                identity.GuestSessionId);
            return FromServiceResult(result);
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

            var result = await _cartService.RemovePromoAsync(
                identity!.UserId,
                identity.GuestSessionId);
            return FromServiceResult(result);
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
                    return (null, UnauthorizedError(identity.ErrorMessage, "Unauthorized"));
                }

                return (null, BadRequestError(identity.ErrorMessage, "ValidationError"));
            }

            return (identity, null);
        }

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
                "InvalidPromo" => BadRequestError(message, code, result.ErrorData),
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
