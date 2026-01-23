using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.DTOs.Reviews;
using sobee_API.Services;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class ReviewServiceTests
{
    [Fact]
    public async Task GetReviewsAsync_InvalidPage_ReturnsValidationError()
    {
        var service = new ReviewService(new FakeReviewRepository(), new FakeProductRepository());

        var result = await service.GetReviewsAsync(productId: 1, page: 0, pageSize: 20);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task GetReviewsAsync_ReturnsSummaryAndOrderedReplies()
    {
        var repo = new FakeReviewRepository();
        var review = repo.AddReview(new Treview
        {
            IntProductId = 1,
            IntRating = 5,
            StrReviewText = "Great",
            DtmReviewDate = new DateTime(2026, 1, 10),
            UserId = "user-1"
        });
        repo.AddReply(new TReviewReplies
        {
            IntReviewId = review.IntReviewId,
            content = "Second",
            created_at = new DateTime(2026, 1, 12),
            UserId = "user-1"
        });
        repo.AddReply(new TReviewReplies
        {
            IntReviewId = review.IntReviewId,
            content = "First",
            created_at = new DateTime(2026, 1, 11),
            UserId = "user-1"
        });

        var service = new ReviewService(repo, new FakeProductRepository());

        var result = await service.GetReviewsAsync(1, page: 1, pageSize: 10);

        result.Success.Should().BeTrue();
        result.Value!.Summary.Total.Should().Be(1);
        result.Value.Summary.Average.Should().Be(5m);
        result.Value.Summary.Counts[4].Should().Be(1);
        result.Value.Reviews.Single().Replies.Select(r => r.Content).Should().ContainInOrder("First", "Second");
    }

    [Fact]
    public async Task CreateReviewAsync_ProductMissing_ReturnsNotFound()
    {
        var service = new ReviewService(new FakeReviewRepository(), new FakeProductRepository());

        var result = await service.CreateReviewAsync(1, "user-1", null, new CreateReviewRequest
        {
            ReviewText = "Test",
            Rating = 5
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task CreateReviewAsync_Valid_AddsReview()
    {
        var repo = new FakeReviewRepository();
        var products = new FakeProductRepository();
        products.AddProduct(1);
        var service = new ReviewService(repo, products);

        var result = await service.CreateReviewAsync(1, "user-1", "session-1", new CreateReviewRequest
        {
            ReviewText = "Nice",
            Rating = 4
        });

        result.Success.Should().BeTrue();
        repo.Reviews.Should().HaveCount(1);
        result.Value!.Rating.Should().Be(4);
    }

    [Fact]
    public async Task CreateReplyAsync_NotOwner_ReturnsForbidden()
    {
        var repo = new FakeReviewRepository();
        repo.AddReview(new Treview
        {
            IntProductId = 1,
            IntRating = 5,
            StrReviewText = "Great",
            DtmReviewDate = DateTime.UtcNow,
            UserId = "owner"
        });
        var service = new ReviewService(repo, new FakeProductRepository());

        var result = await service.CreateReplyAsync(1, "other", isAdmin: false, new CreateReplyRequest { Content = "Nope" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    [Fact]
    public async Task DeleteReviewAsync_RemovesReviewAndReplies()
    {
        var repo = new FakeReviewRepository();
        var review = repo.AddReview(new Treview
        {
            IntProductId = 1,
            IntRating = 3,
            StrReviewText = "Ok",
            DtmReviewDate = DateTime.UtcNow,
            UserId = "user-1"
        });
        repo.AddReply(new TReviewReplies
        {
            IntReviewId = review.IntReviewId,
            content = "Reply",
            created_at = DateTime.UtcNow,
            UserId = "user-1"
        });
        var service = new ReviewService(repo, new FakeProductRepository());

        var result = await service.DeleteReviewAsync(review.IntReviewId, "user-1", isAdmin: false);

        result.Success.Should().BeTrue();
        repo.Reviews.Should().BeEmpty();
        repo.Replies.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteReplyAsync_NotOwner_ReturnsForbidden()
    {
        var repo = new FakeReviewRepository();
        var review = repo.AddReview(new Treview
        {
            IntProductId = 1,
            IntRating = 3,
            StrReviewText = "Ok",
            DtmReviewDate = DateTime.UtcNow,
            UserId = "user-1"
        });
        var reply = repo.AddReply(new TReviewReplies
        {
            IntReviewId = review.IntReviewId,
            content = "Reply",
            created_at = DateTime.UtcNow,
            UserId = "user-1"
        });
        var service = new ReviewService(repo, new FakeProductRepository());

        var result = await service.DeleteReplyAsync(reply.IntReviewReplyID, "other", isAdmin: false);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Forbidden");
    }

    private sealed class FakeReviewRepository : IReviewRepository
    {
        private readonly List<Treview> _reviews = new();
        private readonly List<TReviewReplies> _replies = new();
        private int _nextReviewId = 1;
        private int _nextReplyId = 1;

        public IReadOnlyList<Treview> Reviews => _reviews;
        public IReadOnlyList<TReviewReplies> Replies => _replies;

        public Treview AddReview(Treview review)
        {
            if (review.IntReviewId == 0)
            {
                review.IntReviewId = _nextReviewId++;
            }

            _reviews.Add(review);
            return review;
        }

        public TReviewReplies AddReply(TReviewReplies reply)
        {
            if (reply.IntReviewReplyID == 0)
            {
                reply.IntReviewReplyID = _nextReplyId++;
            }

            _replies.Add(reply);
            var review = _reviews.FirstOrDefault(r => r.IntReviewId == reply.IntReviewId);
            if (review != null)
            {
                review.TReviewReplies.Add(reply);
            }

            return reply;
        }

        public Task<(IReadOnlyList<Treview> Reviews, int TotalCount, IReadOnlyList<(int Rating, int Count)> RatingCounts)> GetByProductAsync(
            int productId,
            int page,
            int pageSize)
        {
            var filtered = _reviews.Where(r => r.IntProductId == productId).ToList();
            var total = filtered.Count;
            var paged = filtered
                .OrderByDescending(r => r.DtmReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var ratingCounts = filtered
                .GroupBy(r => r.IntRating)
                .Select(g => (g.Key, g.Count()))
                .ToList();

            return Task.FromResult(((IReadOnlyList<Treview>)paged, total, (IReadOnlyList<(int, int)>)ratingCounts));
        }

        public Task<Treview?> FindByIdAsync(int reviewId, bool track = true)
            => Task.FromResult(_reviews.FirstOrDefault(r => r.IntReviewId == reviewId));

        public Task<Treview?> FindByIdWithRepliesAsync(int reviewId, bool track = true)
            => Task.FromResult(_reviews.FirstOrDefault(r => r.IntReviewId == reviewId));

        public Task<TReviewReplies?> FindReplyByIdAsync(int replyId, bool track = true)
            => Task.FromResult(_replies.FirstOrDefault(r => r.IntReviewReplyID == replyId));

        public Task AddReviewAsync(Treview review)
        {
            AddReview(review);
            return Task.CompletedTask;
        }

        public Task AddReplyAsync(TReviewReplies reply)
        {
            AddReply(reply);
            return Task.CompletedTask;
        }

        public Task RemoveReviewAsync(Treview review)
        {
            _reviews.Remove(review);
            return Task.CompletedTask;
        }

        public Task RemoveRepliesAsync(IEnumerable<TReviewReplies> replies)
        {
            foreach (var reply in replies.ToList())
            {
                _replies.Remove(reply);
            }

            return Task.CompletedTask;
        }

        public Task RemoveReplyAsync(TReviewReplies reply)
        {
            _replies.Remove(reply);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly HashSet<int> _productIds = new();

        public void AddProduct(int productId)
        {
            _productIds.Add(productId);
        }

        public Task<Tproduct?> FindByIdAsync(int productId)
            => Task.FromResult<Tproduct?>(null);

        public Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true)
            => Task.FromResult<IReadOnlyList<Tproduct>>(Array.Empty<Tproduct>());

        public Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> GetProductsAsync(
            string? query,
            string? category,
            int page,
            int pageSize,
            string? sort,
            bool track = false)
            => Task.FromResult(((IReadOnlyList<Tproduct>)Array.Empty<Tproduct>(), 0));

        public Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false)
            => Task.FromResult<Tproduct?>(null);

        public Task<bool> ExistsAsync(int productId)
            => Task.FromResult(_productIds.Contains(productId));

        public Task<TproductImage?> FindImageAsync(int productId, int imageId)
            => Task.FromResult<TproductImage?>(null);

        public Task AddAsync(Tproduct product)
            => Task.CompletedTask;

        public Task AddImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task RemoveAsync(Tproduct product)
            => Task.CompletedTask;

        public Task RemoveImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
