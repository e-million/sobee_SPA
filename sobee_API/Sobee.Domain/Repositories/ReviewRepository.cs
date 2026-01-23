using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Reviews;

namespace Sobee.Domain.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly SobeecoredbContext _db;

    public ReviewRepository(SobeecoredbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Treview> Reviews, int TotalCount, IReadOnlyList<(int Rating, int Count)> RatingCounts)> GetByProductAsync(
        int productId,
        int page,
        int pageSize)
    {
        var baseQuery = _db.Treviews
            .AsNoTracking()
            .Where(r => r.IntProductId == productId);

        var totalCount = await baseQuery.CountAsync();

        var ratingGroups = await baseQuery
            .GroupBy(r => r.IntRating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var ratingCounts = ratingGroups
            .Select(g => (g.Rating, g.Count))
            .ToList();

        var reviews = await baseQuery
            .Include(r => r.TReviewReplies)
            .OrderByDescending(r => r.DtmReviewDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount, ratingCounts);
    }

    public async Task<Treview?> FindByIdAsync(int reviewId, bool track = true)
    {
        var query = _db.Treviews.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
    }

    public async Task<Treview?> FindByIdWithRepliesAsync(int reviewId, bool track = true)
    {
        var query = _db.Treviews
            .Include(r => r.TReviewReplies)
            .AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(r => r.IntReviewId == reviewId);
    }

    public async Task<TReviewReplies?> FindReplyByIdAsync(int replyId, bool track = true)
    {
        var query = _db.TReviewReplies.AsQueryable();
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(r => r.IntReviewReplyID == replyId);
    }

    public async Task AddReviewAsync(Treview review)
    {
        _db.Treviews.Add(review);
        await Task.CompletedTask;
    }

    public async Task AddReplyAsync(TReviewReplies reply)
    {
        _db.TReviewReplies.Add(reply);
        await Task.CompletedTask;
    }

    public async Task RemoveReviewAsync(Treview review)
    {
        _db.Treviews.Remove(review);
        await Task.CompletedTask;
    }

    public async Task RemoveRepliesAsync(IEnumerable<TReviewReplies> replies)
    {
        _db.TReviewReplies.RemoveRange(replies);
        await Task.CompletedTask;
    }

    public async Task RemoveReplyAsync(TReviewReplies reply)
    {
        _db.TReviewReplies.Remove(reply);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
