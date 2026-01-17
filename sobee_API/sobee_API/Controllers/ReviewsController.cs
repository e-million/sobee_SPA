using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
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
        public async Task<IActionResult> GetByProduct(int productId)
        {
            // Public endpoint: do not create guest sessions.
            _ = await _identity.ResolveAsync(
                User,
                Request,
                Response,
                allowCreateGuestSession: false,
                allowAuthenticatedGuestSession: false
            );

            var reviews = await _db.Treviews
                .Where(r => r.IntProductId == productId)
                .OrderByDescending(r => r.DtmReviewDate)
                .Select(r => new
                {
                    reviewId = r.IntReviewId,
                    productId = r.IntProductId,
                    rating = r.IntRating,
                    reviewText = r.StrReviewText,
                    created = r.DtmReviewDate,
                    userId = r.UserId,
                    sessionId = r.SessionId,
                    replies = _db.TReviewReplies
                        .Where(rr => rr.IntReviewId == r.IntReviewId)
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
                })
                .ToListAsync();

            return Ok(new
            {
                productId,
                count = reviews.Count,
                reviews
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
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            // Validate product exists
            var productExists = await _db.Tproducts.AnyAsync(p => p.IntProductId == productId);
            if (!productExists)
                return NotFound(new { error = "Product not found.", productId });

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
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var review = await _db.Treviews.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
            if (review == null)
                return NotFound(new { error = "Review not found.", reviewId });

            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == owner.UserId;

            if (!isAdmin && !isOwner)
                return Forbid();

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
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var review = await _db.Treviews.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
            if (review == null)
                return NotFound(new { error = "Review not found.", reviewId });

            // Owner check or Admin role
            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == owner.UserId;

            if (!isOwner && !isAdmin)
                return Forbid();

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
                return Unauthorized(new { error = "Missing NameIdentifier claim." });

            var reply = await _db.TReviewReplies.FirstOrDefaultAsync(r => r.IntReviewReplyID == replyId);
            if (reply == null)
                return NotFound(new { error = "Reply not found.", replyId });

            var isAdmin = User.IsInRole("Admin");
            var isOwner = !string.IsNullOrWhiteSpace(reply.UserId) && reply.UserId == owner.UserId;

            if (!isOwner && !isAdmin)
                return Forbid();

            _db.TReviewReplies.Remove(reply);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Reply deleted.", replyId });
        }
    }
}
