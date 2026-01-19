using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using sobee_API.DTOs.Common;
using sobee_API.DTOs.Reviews;
using sobee_API.Services;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly SobeecoredbContext _db;
        private readonly RequestIdentityResolver _identity;

        public ReviewsController(SobeecoredbContext db, RequestIdentityResolver identity)
        {
            _db = db;
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
            if (page <= 0)
                return BadRequest(new ApiErrorResponse("page must be >= 1", "ValidationError"));

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new ApiErrorResponse("pageSize must be between 1 and 100", "ValidationError"));

            // Public endpoint: do not create guest sessions.
            _ = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            var query = _db.Treviews
                .Include(r => r.TReviewReplies)
                .AsNoTracking()
                .Where(r => r.IntProductId == productId);

            var totalCount = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.DtmReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var results = reviews.Select(r => new
            {
                reviewId = r.IntReviewId,
                productId = r.IntProductId,
                rating = r.IntRating,
                reviewText = r.StrReviewText,
                created = r.DtmReviewDate,
                userId = r.UserId,
                sessionId = r.SessionId,
                replies = (r.TReviewReplies ?? [])
                    .OrderBy(rr => rr.created_at)
                    .Select(rr => new
                    {
                        replyId = rr.IntReviewReplyID,
                        reviewId = rr.IntReviewId,
                        content = rr.content,
                        created = rr.created_at,
                        userId = rr.UserId
                    })
                    .ToList()
            }).ToList();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(new
            {
                productId,
                count = results.Count,
                reviews = results
            });
        }

        /// <summary>
        /// Create a review for a product (authenticated only).
        /// </summary>
        [Authorize]
        [HttpPost("product/{productId:int}")]
        public async Task<IActionResult> Create(int productId, [FromBody] CreateReviewRequest request)
        {
            // Do not auto-create guest sessions here (prevents GuestSessions table abuse).
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            // Must be either authenticated OR a valid existing guest session.
            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new ApiErrorResponse("Missing NameIdentifier claim.", "Unauthorized"));

            // Validate product exists
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == productId);
            if (!productExists)
                return NotFound(new ApiErrorResponse("Product not found.", "NotFound", new { productId }));

            var review = new Sobee.Domain.Entities.Reviews.Treview
            {
                IntProductId = productId,
                StrReviewText = request.ReviewText!,
                IntRating = request.Rating,
                DtmReviewDate = DateTime.UtcNow,
                UserId = owner.UserId,
                SessionId = owner.GuestSessionId
            };

            _db.Treviews.Add(review);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Review created.",
                reviewId = review.IntReviewId,
                productId = review.IntProductId,
                rating = review.IntRating,
                reviewText = review.StrReviewText,
                created = review.DtmReviewDate,
                userId = review.UserId,
                sessionId = review.SessionId
            });
        }

        /// <summary>
        /// Reply to a review (authenticated only).
        /// </summary>
        [HttpPost("{reviewId:int}/reply")]
        [Authorize]
        public async Task<IActionResult> Reply(int reviewId, [FromBody] CreateReplyRequest request)
        {
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new ApiErrorResponse("Missing NameIdentifier claim.", "Unauthorized"));

            var review = await _db.Treviews.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
            if (review == null)
                return NotFound(new ApiErrorResponse("Review not found.", "NotFound", new { reviewId }));

            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == owner.UserId;

            if (!isAdmin && !isOwner)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse("Forbidden.", "Forbidden"));

            var reply = new Sobee.Domain.Entities.Reviews.TReviewReplies
            {
                IntReviewId = reviewId,
                content = request.Content!,
                created_at = DateTime.UtcNow,
                UserId = owner.UserId
            };

            _db.TReviewReplies.Add(reply);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Reply created.",
                replyId = reply.IntReviewReplyID,
                reviewId = reply.IntReviewId,
                content = reply.content,
                created = reply.created_at,
                userId = reply.UserId
            });
        }

        /// <summary>
        /// Delete a review (owner or admin).
        /// </summary>
        [HttpDelete("{reviewId:int}")]
        [Authorize] // keep destructive actions authenticated
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new ApiErrorResponse("Missing NameIdentifier claim.", "Unauthorized"));

            var review = await _db.Treviews.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
            if (review == null)
                return NotFound(new ApiErrorResponse("Review not found.", "NotFound", new { reviewId }));

            // Owner check or Admin role
            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == owner.UserId;

            if (!isOwner && !isAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse("Forbidden.", "Forbidden"));

            // Remove replies first (if FK doesn't cascade)
            var replies = await _db.TReviewReplies.Where(rr => rr.IntReviewId == reviewId).ToListAsync();
            if (replies.Count > 0)
                _db.TReviewReplies.RemoveRange(replies);

            _db.Treviews.Remove(review);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Review deleted.", reviewId });
        }

        /// <summary>
        /// Delete a reply (author or admin).
        /// </summary>
        [HttpDelete("replies/{replyId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var owner = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            if (string.IsNullOrWhiteSpace(owner.UserId))
                return Unauthorized(new ApiErrorResponse("Missing NameIdentifier claim.", "Unauthorized"));

            var reply = await _db.TReviewReplies.FirstOrDefaultAsync(r => r.IntReviewReplyID == replyId);
            if (reply == null)
                return NotFound(new ApiErrorResponse("Reply not found.", "NotFound", new { replyId }));

            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(reply.UserId) && reply.UserId == owner.UserId;

            if (!isOwner && !isAdmin)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse("Forbidden.", "Forbidden"));

            _db.TReviewReplies.Remove(reply);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Reply deleted.", replyId });
        }
    }
}
