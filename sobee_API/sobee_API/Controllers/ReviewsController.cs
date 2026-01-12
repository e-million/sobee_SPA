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
                .Select(r => new ReviewListItemDto
                {
                    ReviewId = r.IntReviewId,
                    ProductId = r.IntProductId,
                    Rating = r.IntRating,
                    ReviewText = r.StrReviewText,
                    Created = r.DtmReviewDate,
                    UserId = r.UserId,
                    SessionId = r.SessionId,
                    Replies = _db.TReviewReplies
                        .Where(rr => rr.IntReviewId == r.IntReviewId)
                        .OrderBy(rr => rr.created_at)
                        .Select(rr => new ReviewReplyDto
                        {
                            ReplyId = rr.IntReviewReplyID,
                            ReviewId = rr.IntReviewId,
                            Content = rr.content,
                            Created = rr.created_at,
                            UserId = rr.UserId
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(new ProductReviewsResponseDto
            {
                ProductId = productId,
                Count = reviews.Count,
                Reviews = reviews
            });
        }

        /// <summary>
        /// Create a review for a product (authenticated or valid guest session).
        /// </summary>
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
            if (string.IsNullOrWhiteSpace(owner.UserId) && string.IsNullOrWhiteSpace(owner.GuestSessionId))
            {
                return Forbid();
            }

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

            var response = new ReviewCreatedResponseDto
            {
                Message = "Review created.",
                ReviewId = review.IntReviewId,
                ProductId = review.IntProductId,
                Rating = review.IntRating,
                ReviewText = review.StrReviewText,
                Created = review.DtmReviewDate,
                UserId = review.UserId,
                SessionId = review.SessionId
            };

            return CreatedAtAction(nameof(GetByProduct), new { productId }, response);
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

            var reply = new Sobee.Domain.Entities.Reviews.TReviewReplies
            {
                IntReviewId = reviewId,
                content = request.Content!,
                created_at = DateTime.UtcNow,
                UserId = owner.UserId
            };

            _db.TReviewReplies.Add(reply);
            await _db.SaveChangesAsync();

            var response = new ReviewReplyCreatedResponseDto
            {
                Message = "Reply created.",
                ReplyId = reply.IntReviewReplyID,
                ReviewId = reply.IntReviewId,
                Content = reply.content,
                Created = reply.created_at,
                UserId = reply.UserId
            };

            return CreatedAtAction(nameof(GetByProduct), new { productId = review.IntProductId }, response);
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

            return Ok(new ReviewDeletedResponseDto { Message = "Review deleted.", ReviewId = reviewId });
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

            return Ok(new ReviewReplyDeletedResponseDto { Message = "Reply deleted.", ReplyId = replyId });
        }
    }
}
