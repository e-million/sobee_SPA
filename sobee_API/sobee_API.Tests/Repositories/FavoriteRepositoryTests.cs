using System;
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

public class FavoriteRepositoryTests
{
    [Fact]
    public async Task GetByUserAsync_ReturnsPagedResults()
    {
        using var context = new SqliteTestContext();
        var product1 = context.AddProduct(CreateProduct(1));
        var product2 = context.AddProduct(CreateProduct(2));
        context.AddFavorite(CreateFavorite(product1.IntProductId, "user-1", DateTime.UtcNow.AddMinutes(-10)));
        context.AddFavorite(CreateFavorite(product2.IntProductId, "user-1", DateTime.UtcNow));

        var (items, total) = await context.Repository.GetByUserAsync("user-1", page: 1, pageSize: 1);

        total.Should().Be(2);
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindByUserAndProductAsync_ReturnsFavorite()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1));
        context.AddFavorite(CreateFavorite(product.IntProductId, "user-1", DateTime.UtcNow));

        var result = await context.Repository.FindByUserAndProductAsync("user-1", product.IntProductId, track: false);

        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1));
        context.AddFavorite(CreateFavorite(product.IntProductId, "user-1", DateTime.UtcNow));

        var exists = await context.Repository.ExistsAsync("user-1", product.IntProductId);

        exists.Should().BeTrue();
    }

    private static Tproduct CreateProduct(int id)
        => new()
        {
            IntProductId = id,
            StrName = $"Product-{id}",
            strDescription = $"Product-{id}",
            DecPrice = 5m,
            IntStockAmount = 10
        };

    private static Tfavorite CreateFavorite(int productId, string userId, DateTime added)
        => new()
        {
            IntProductId = productId,
            UserId = userId,
            DtmDateAdded = added
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public FavoriteRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new FavoriteRepository(DbContext);
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

        public Tfavorite AddFavorite(Tfavorite favorite)
        {
            DbContext.Tfavorites.Add(favorite);
            DbContext.SaveChanges();
            return favorite;
        }
    }
}
