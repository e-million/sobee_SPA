using Sobee.Domain.Entities.Reviews;

namespace Sobee.Domain.Repositories;

public interface IReviewRepository
{
    Task<(IReadOnlyList<Treview> Reviews, int TotalCount, IReadOnlyList<(int Rating, int Count)> RatingCounts)> GetByProductAsync(
        int productId,
        int page,
        int pageSize);
    Task<Treview?> FindByIdAsync(int reviewId, bool track = true);
    Task<Treview?> FindByIdWithRepliesAsync(int reviewId, bool track = true);
    Task<TReviewReplies?> FindReplyByIdAsync(int replyId, bool track = true);
    Task AddReviewAsync(Treview review);
    Task AddReplyAsync(TReviewReplies reply);
    Task RemoveReviewAsync(Treview review);
    Task RemoveRepliesAsync(IEnumerable<TReviewReplies> replies);
    Task RemoveReplyAsync(TReviewReplies reply);
    Task SaveChangesAsync();
}
