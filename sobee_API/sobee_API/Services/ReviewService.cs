using sobee_API.Domain;
using sobee_API.DTOs.Reviews;
using sobee_API.Mapping;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;

namespace sobee_API.Services;

public sealed class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;

    public ReviewService(IReviewRepository reviewRepository, IProductRepository productRepository)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<ReviewListResponseDto>> GetReviewsAsync(int productId, int page, int pageSize)
    {
        if (page <= 0)
        {
            return Validation<ReviewListResponseDto>("page must be >= 1", new { page });
        }

        if (pageSize <= 0 || pageSize > 100)
        {
            return Validation<ReviewListResponseDto>("pageSize must be between 1 and 100", new { pageSize });
        }

        var (reviews, totalCount, ratingCounts) = await _reviewRepository.GetByProductAsync(productId, page, pageSize);

        var counts = new int[5];
        var totalRating = 0;

        foreach (var (rating, count) in ratingCounts)
        {
            if (rating >= 1 && rating <= 5)
            {
                counts[rating - 1] = count;
                totalRating += rating * count;
            }
        }

        var average = totalCount == 0 ? 0m : (decimal)totalRating / totalCount;

        var response = new ReviewListResponseDto
        {
            ProductId = productId,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Count = reviews.Count,
            Summary = new ReviewSummaryDto
            {
                Total = totalCount,
                Average = average,
                Counts = counts
            }
        };

        foreach (var review in reviews)
        {
            response.Reviews.Add(review.ToReviewResponseDto());
        }

        return ServiceResult<ReviewListResponseDto>.Ok(response);
    }

    public async Task<ServiceResult<ReviewCreatedResponseDto>> CreateReviewAsync(
        int productId,
        string? userId,
        string? sessionId,
        CreateReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<ReviewCreatedResponseDto>("Missing NameIdentifier claim.", null);
        }

        if (string.IsNullOrWhiteSpace(request.ReviewText))
        {
            return Validation<ReviewCreatedResponseDto>("ReviewText is required.", new { field = "reviewText" });
        }

        if (request.Rating is < 1 or > 5)
        {
            return Validation<ReviewCreatedResponseDto>("Rating must be between 1 and 5.", new { field = "rating" });
        }

        var productExists = await _productRepository.ExistsAsync(productId);
        if (!productExists)
        {
            return NotFound<ReviewCreatedResponseDto>("Product not found.", new { productId });
        }

        var review = new Treview
        {
            IntProductId = productId,
            StrReviewText = request.ReviewText!,
            IntRating = request.Rating,
            DtmReviewDate = DateTime.UtcNow,
            UserId = userId,
            SessionId = sessionId
        };

        await _reviewRepository.AddReviewAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return ServiceResult<ReviewCreatedResponseDto>.Ok(new ReviewCreatedResponseDto
        {
            Message = "Review created.",
            ReviewId = review.IntReviewId,
            ProductId = review.IntProductId,
            Rating = review.IntRating,
            ReviewText = review.StrReviewText,
            Created = review.DtmReviewDate,
            UserId = review.UserId,
            SessionId = review.SessionId
        });
    }

    public async Task<ServiceResult<ReplyCreatedResponseDto>> CreateReplyAsync(
        int reviewId,
        string? userId,
        bool isAdmin,
        CreateReplyRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<ReplyCreatedResponseDto>("Missing NameIdentifier claim.", null);
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Validation<ReplyCreatedResponseDto>("Content is required.", new { field = "content" });
        }

        var review = await _reviewRepository.FindByIdAsync(reviewId, track: false);
        if (review == null)
        {
            return NotFound<ReplyCreatedResponseDto>("Review not found.", new { reviewId });
        }

        var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == userId;
        if (!isAdmin && !isOwner)
        {
            return Forbidden<ReplyCreatedResponseDto>("Forbidden.", null);
        }

        var reply = new TReviewReplies
        {
            IntReviewId = reviewId,
            content = request.Content!,
            created_at = DateTime.UtcNow,
            UserId = userId
        };

        await _reviewRepository.AddReplyAsync(reply);
        await _reviewRepository.SaveChangesAsync();

        return ServiceResult<ReplyCreatedResponseDto>.Ok(new ReplyCreatedResponseDto
        {
            Message = "Reply created.",
            ReplyId = reply.IntReviewReplyID,
            ReviewId = reply.IntReviewId,
            Content = reply.content,
            Created = reply.created_at,
            UserId = reply.UserId
        });
    }

    public async Task<ServiceResult<ReviewDeletedResponseDto>> DeleteReviewAsync(int reviewId, string? userId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<ReviewDeletedResponseDto>("Missing NameIdentifier claim.", null);
        }

        var review = await _reviewRepository.FindByIdWithRepliesAsync(reviewId, track: true);
        if (review == null)
        {
            return NotFound<ReviewDeletedResponseDto>("Review not found.", new { reviewId });
        }

        var isOwner = !string.IsNullOrWhiteSpace(review.UserId) && review.UserId == userId;
        if (!isOwner && !isAdmin)
        {
            return Forbidden<ReviewDeletedResponseDto>("Forbidden.", null);
        }

        if (review.TReviewReplies.Count > 0)
        {
            await _reviewRepository.RemoveRepliesAsync(review.TReviewReplies);
        }

        await _reviewRepository.RemoveReviewAsync(review);
        await _reviewRepository.SaveChangesAsync();

        return ServiceResult<ReviewDeletedResponseDto>.Ok(new ReviewDeletedResponseDto
        {
            Message = "Review deleted.",
            ReviewId = reviewId
        });
    }

    public async Task<ServiceResult<ReplyDeletedResponseDto>> DeleteReplyAsync(int replyId, string? userId, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized<ReplyDeletedResponseDto>("Missing NameIdentifier claim.", null);
        }

        var reply = await _reviewRepository.FindReplyByIdAsync(replyId, track: true);
        if (reply == null)
        {
            return NotFound<ReplyDeletedResponseDto>("Reply not found.", new { replyId });
        }

        var isOwner = !string.IsNullOrWhiteSpace(reply.UserId) && reply.UserId == userId;
        if (!isOwner && !isAdmin)
        {
            return Forbidden<ReplyDeletedResponseDto>("Forbidden.", null);
        }

        await _reviewRepository.RemoveReplyAsync(reply);
        await _reviewRepository.SaveChangesAsync();

        return ServiceResult<ReplyDeletedResponseDto>.Ok(new ReplyDeletedResponseDto
        {
            Message = "Reply deleted.",
            ReplyId = replyId
        });
    }

    private static ServiceResult<T> NotFound<T>(string message, object? data)
        => ServiceResult<T>.Fail("NotFound", message, data);

    private static ServiceResult<T> Validation<T>(string message, object? data)
        => ServiceResult<T>.Fail("ValidationError", message, data);

    private static ServiceResult<T> Unauthorized<T>(string message, object? data)
        => ServiceResult<T>.Fail("Unauthorized", message, data);

    private static ServiceResult<T> Forbidden<T>(string message, object? data)
        => ServiceResult<T>.Fail("Forbidden", message, data);
}
