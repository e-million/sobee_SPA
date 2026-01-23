using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Services;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Entities.Reviews;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class FavoriteServiceTests
{
    [Fact]
    public async Task GetFavoritesAsync_MissingUser_ReturnsUnauthorized()
    {
        var service = new FavoriteService(new FakeFavoriteRepository(), new FakeProductRepository());

        var result = await service.GetFavoritesAsync(null, page: 1, pageSize: 20);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task GetFavoritesAsync_ReturnsPagedResults()
    {
        var repo = new FakeFavoriteRepository();
        repo.AddFavorite(new Tfavorite
        {
            IntFavoriteId = 1,
            IntProductId = 10,
            DtmDateAdded = DateTime.UtcNow,
            UserId = "user-1"
        });

        var service = new FavoriteService(repo, new FakeProductRepository());

        var result = await service.GetFavoritesAsync("user-1", page: 1, pageSize: 10);

        result.Success.Should().BeTrue();
        result.Value!.Count.Should().Be(1);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task AddFavoriteAsync_ProductMissing_ReturnsNotFound()
    {
        var service = new FavoriteService(new FakeFavoriteRepository(), new FakeProductRepository());

        var result = await service.AddFavoriteAsync("user-1", 99);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task AddFavoriteAsync_AlreadyExists_ReturnsMessage()
    {
        var repo = new FakeFavoriteRepository();
        repo.AddFavorite(new Tfavorite { IntProductId = 1, UserId = "user-1" });
        var products = new FakeProductRepository();
        products.AddProduct(1);
        var service = new FavoriteService(repo, products);

        var result = await service.AddFavoriteAsync("user-1", 1);

        result.Success.Should().BeTrue();
        result.Value!.Message.Should().Be("Already favorited.");
        result.Value.FavoriteId.Should().BeNull();
    }

    [Fact]
    public async Task AddFavoriteAsync_Valid_AddsFavorite()
    {
        var repo = new FakeFavoriteRepository();
        var products = new FakeProductRepository();
        products.AddProduct(1);
        var service = new FavoriteService(repo, products);

        var result = await service.AddFavoriteAsync("user-1", 1);

        result.Success.Should().BeTrue();
        repo.Favorites.Should().ContainSingle(f => f.IntProductId == 1);
        result.Value!.FavoriteId.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveFavoriteAsync_NotFound_ReturnsNotFound()
    {
        var service = new FavoriteService(new FakeFavoriteRepository(), new FakeProductRepository());

        var result = await service.RemoveFavoriteAsync("user-1", 1);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    private sealed class FakeFavoriteRepository : IFavoriteRepository
    {
        private readonly List<Tfavorite> _favorites = new();
        private int _nextId = 1;

        public IReadOnlyList<Tfavorite> Favorites => _favorites;

        public void AddFavorite(Tfavorite favorite)
        {
            if (favorite.IntFavoriteId == 0)
            {
                favorite.IntFavoriteId = _nextId++;
            }

            _favorites.Add(favorite);
        }

        public Task<(IReadOnlyList<Tfavorite> Items, int TotalCount)> GetByUserAsync(string userId, int page, int pageSize)
        {
            var query = _favorites.Where(f => f.UserId == userId);
            var total = query.Count();
            var items = query
                .OrderByDescending(f => f.DtmDateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(((IReadOnlyList<Tfavorite>)items, total));
        }

        public Task<Tfavorite?> FindByUserAndProductAsync(string userId, int productId, bool track = true)
            => Task.FromResult(_favorites.FirstOrDefault(f => f.UserId == userId && f.IntProductId == productId));

        public Task<bool> ExistsAsync(string userId, int productId)
            => Task.FromResult(_favorites.Any(f => f.UserId == userId && f.IntProductId == productId));

        public Task AddAsync(Tfavorite favorite)
        {
            AddFavorite(favorite);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Tfavorite favorite)
        {
            _favorites.Remove(favorite);
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
