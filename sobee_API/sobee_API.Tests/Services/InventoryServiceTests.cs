using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.Domain;
using sobee_API.Services;
using sobee_API.Services.Interfaces;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task ValidateAndDecrementAsync_Empty_ReturnsOk()
    {
        var service = new InventoryService(new FakeProductRepository());

        var result = await service.ValidateAndDecrementAsync(new List<InventoryRequestItem>());

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndDecrementAsync_MissingProduct_ReturnsValidationError()
    {
        var service = new InventoryService(new FakeProductRepository());

        var result = await service.ValidateAndDecrementAsync(new List<InventoryRequestItem>
        {
            new(1, 1)
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ValidationError");
    }

    [Fact]
    public async Task ValidateAndDecrementAsync_InsufficientStock_ReturnsError()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(new Tproduct { IntProductId = 1, StrName = "Product-1", strDescription = "Product-1", DecPrice = 5m, IntStockAmount = 1 });
        var service = new InventoryService(repository);

        var result = await service.ValidateAndDecrementAsync(new List<InventoryRequestItem>
        {
            new(1, 2)
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task ValidateAndDecrementAsync_Valid_DecrementsStock()
    {
        var repository = new FakeProductRepository();
        var product = repository.AddProduct(new Tproduct { IntProductId = 1, StrName = "Product-1", strDescription = "Product-1", DecPrice = 5m, IntStockAmount = 5 });
        var service = new InventoryService(repository);

        var result = await service.ValidateAndDecrementAsync(new List<InventoryRequestItem>
        {
            new(1, 2),
            new(1, 1)
        });

        result.Success.Should().BeTrue();
        product.IntStockAmount.Should().Be(2);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<int, Tproduct> _products = new();

        public Tproduct AddProduct(Tproduct product)
        {
            _products[product.IntProductId] = product;
            return product;
        }

        public Task<Tproduct?> FindByIdAsync(int productId, bool track = true)
            => Task.FromResult(_products.TryGetValue(productId, out var product) ? product : null);

        public Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true)
        {
            IReadOnlyList<Tproduct> products = productIds
                .Distinct()
                .Select(id => _products.TryGetValue(id, out var product) ? product : null)
                .Where(product => product != null)
                .Cast<Tproduct>()
                .ToList();

            return Task.FromResult(products);
        }

        public Task<(IReadOnlyList<Tproduct> Items, int TotalCount)> GetProductsAsync(
            string? query,
            string? category,
            int page,
            int pageSize,
            string? sort,
            bool track = false)
        {
            IReadOnlyList<Tproduct> products = _products.Values.ToList();
            return Task.FromResult((products, products.Count));
        }

        public Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false)
            => FindByIdAsync(productId);

        public Task<bool> ExistsAsync(int productId)
            => Task.FromResult(_products.ContainsKey(productId));

        public Task<TproductImage?> FindImageAsync(int productId, int imageId)
            => Task.FromResult<TproductImage?>(null);

        public Task AddAsync(Tproduct product)
        {
            _products[product.IntProductId] = product;
            return Task.CompletedTask;
        }

        public Task AddImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task RemoveAsync(Tproduct product)
        {
            _products.Remove(product.IntProductId);
            return Task.CompletedTask;
        }

        public Task RemoveImageAsync(TproductImage image)
            => Task.CompletedTask;

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
