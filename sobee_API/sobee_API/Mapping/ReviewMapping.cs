using System.Linq;
using sobee_API.DTOs.Reviews;
using Sobee.Domain.Entities.Reviews;

namespace sobee_API.Mapping;

public static class ReviewMapping
{
    public static ReviewResponseDto ToReviewResponseDto(this Treview review)
    {
        var response = new ReviewResponseDto
        {
            ReviewId = review.IntReviewId,
            ProductId = review.IntProductId,
            Rating = review.IntRating,
            ReviewText = review.StrReviewText,
            Created = review.DtmReviewDate,
            UserId = review.UserId,
            SessionId = review.SessionId
        };

        if (review.TReviewReplies != null)
        {
            response.Replies = review.TReviewReplies
                .OrderBy(reply => reply.created_at)
                .Select(ToReviewReplyDto)
                .ToList();
        }

        return response;
    }

    public static ReviewReplyDto ToReviewReplyDto(this TReviewReplies reply)
    {
        return new ReviewReplyDto
        {
            ReplyId = reply.IntReviewReplyID,
            ReviewId = reply.IntReviewId,
            Content = reply.content,
            Created = reply.created_at,
            UserId = reply.UserId
        };
    }
}
