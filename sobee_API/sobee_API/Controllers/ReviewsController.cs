using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sobee_API.Domain;
using sobee_API.DTOs.Reviews;
using sobee_API.Services;
using sobee_API.Services.Interfaces;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ApiControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly RequestIdentityResolver _identity;

        public ReviewsController(IReviewService reviewService, RequestIdentityResolver identity)
        {
            _reviewService = reviewService;
            _identity = identity;
        }

        /// <summary>
        /// Get reviews for a product (public).
        /// </summary>
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(
            int productId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Public endpoint: do not create guest sessions.
            _ = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            var result = await _reviewService.GetReviewsAsync(productId, page, pageSize);
            if (!result.Success)
            {
                return FromServiceResult(result);
            }

            Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString();
            Response.Headers["X-Page"] = result.Value.Page.ToString();
            Response.Headers["X-Page-Size"] = result.Value.PageSize.ToString();

            return Ok(result.Value);
        }

        /// <summary>
        /// Create a review for a product (authenticated only).
        /// </summary>
        [Authorize]
        [HttpPost("product/{productId:int}")]
        public async Task<IActionResult> Create(int productId, [FromBody] CreateReviewRequest request)
        {
            // Do not auto-create guest sessions here (prevents GuestSessions table abuse).
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _reviewService.CreateReviewAsync(
                productId,
                identity!.UserId,
                identity.GuestSessionId,
                request);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Reply to a review (authenticated only).
        /// </summary>
        [HttpPost("{reviewId:int}/reply")]
        [Authorize]
        public async Task<IActionResult> Reply(int reviewId, [FromBody] CreateReplyRequest request)
        {
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _reviewService.CreateReplyAsync(
                reviewId,
                identity!.UserId,
                User.IsInRole("Admin"),
                request);
            return FromServiceResult(result);
        }

        /// <summary>
        /// Delete a review (owner or admin).
        /// </summary>
        [HttpDelete("{reviewId:int}")]
        [Authorize] // keep destructive actions authenticated
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _reviewService.DeleteReviewAsync(
                reviewId,
                identity!.UserId,
                User.IsInRole("Admin"));
            return FromServiceResult(result);
        }

        /// <summary>
        /// Delete a reply (author or admin).
        /// </summary>
        [HttpDelete("replies/{replyId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var (identity, errorResult) = await ResolveIdentityAsync();
            if (errorResult != null)
            {
                return errorResult;
            }

            var result = await _reviewService.DeleteReplyAsync(
                replyId,
                identity!.UserId,
                User.IsInRole("Admin"));
            return FromServiceResult(result);
        }

        private async Task<(RequestIdentity? identity, IActionResult? errorResult)> ResolveIdentityAsync()
        {
            var identity = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false);

            if (identity.HasError)
            {
                if (identity.ErrorCode == "MissingNameIdentifier")
                {
                    return (null, UnauthorizedError(identity.ErrorMessage ?? "Unauthorized", "Unauthorized"));
                }

                return (null, BadRequestError(identity.ErrorMessage ?? "Invalid request", "ValidationError"));
            }

            return (identity, null);
        }

    }
}
