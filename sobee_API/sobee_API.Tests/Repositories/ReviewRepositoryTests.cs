using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class ReviewRepositoryTests
{
    [Fact]
    public async Task GetByProductAsync_ReturnsRatingsAndReplies()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1));
        var review1 = context.AddReview(CreateReview(product.IntProductId, 5, DateTime.UtcNow.AddDays(-1)));
        var review2 = context.AddReview(CreateReview(product.IntProductId, 3, DateTime.UtcNow));
        context.AddReply(CreateReply(review1.IntReviewId));

        var (reviews, total, ratingCounts) = await context.Repository.GetByProductAsync(product.IntProductId, page: 1, pageSize: 10);

        total.Should().Be(2);
        reviews.Should().HaveCount(2);
        ratingCounts.Should().Contain(rc => rc.Rating == 5 && rc.Count == 1);
        ratingCounts.Should().Contain(rc => rc.Rating == 3 && rc.Count == 1);
        reviews.SelectMany(r => r.TReviewReplies).Should().NotBeEmpty();
    }

    [Fact]
    public async Task FindByIdWithRepliesAsync_ReturnsReplies()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1));
        var review = context.AddReview(CreateReview(product.IntProductId, 4, DateTime.UtcNow));
        context.AddReply(CreateReply(review.IntReviewId));

        var result = await context.Repository.FindByIdWithRepliesAsync(review.IntReviewId, track: false);

        result.Should().NotBeNull();
        result!.TReviewReplies.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindReplyByIdAsync_ReturnsReply()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1));
        var review = context.AddReview(CreateReview(product.IntProductId, 2, DateTime.UtcNow));
        var reply = context.AddReply(CreateReply(review.IntReviewId));

        var result = await context.Repository.FindReplyByIdAsync(reply.IntReviewReplyID, track: false);

        result.Should().NotBeNull();
        result!.IntReviewId.Should().Be(review.IntReviewId);
    }

    private static Tproduct CreateProduct(int id)
        => new()
        {
            IntProductId = id,
            StrName = $"Product-{id}",
            strDescription = $"Product-{id}",
            DecPrice = 4m,
            IntStockAmount = 10
        };

    private static Treview CreateReview(int productId, int rating, DateTime date)
        => new()
        {
            IntProductId = productId,
            IntRating = rating,
            StrReviewText = "Review",
            DtmReviewDate = date,
            UserId = "user-1"
        };

    private static TReviewReplies CreateReply(int reviewId)
        => new()
        {
            IntReviewId = reviewId,
            UserId = "user-1",
            content = "Reply",
            created_at = DateTime.UtcNow
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public ReviewRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new ReviewRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public Tproduct AddProduct(Tproduct product)
        {
            DbContext.Tproducts.Add(product);
            DbContext.SaveChanges();
            return product;
        }

        public Treview AddReview(Treview review)
        {
            DbContext.Treviews.Add(review);
            DbContext.SaveChanges();
            return review;
        }

        public TReviewReplies AddReply(TReviewReplies reply)
        {
            DbContext.TReviewReplies.Add(reply);
            DbContext.SaveChanges();
            return reply;
        }
    }
}
