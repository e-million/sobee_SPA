using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using sobee_API.DTOs.Products;
using sobee_API.Services;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProductsAsync_ReturnsPagedResults()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "A", 1m));
        repository.AddProduct(CreateProduct(2, "B", 2m));
        repository.AddProduct(CreateProduct(3, "C", 3m));
        var service = new ProductService(repository);

        var result = await service.GetProductsAsync(null, null, page: 1, pageSize: 2, sort: null, isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetProductsAsync_WithSearch_FiltersCorrectly()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Low Sugar", 1m));
        repository.AddProduct(CreateProduct(2, "High Sugar", 2m));
        var service = new ProductService(repository);

        var result = await service.GetProductsAsync("Low", null, page: 1, pageSize: 10, sort: null, isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Contain("Low");
    }

    [Fact]
    public async Task GetProductsAsync_WithCategory_FiltersCorrectly()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Tea", 1m, category: "Tea"));
        repository.AddProduct(CreateProduct(2, "Coffee", 2m, category: "Coffee"));
        var service = new ProductService(repository);

        var result = await service.GetProductsAsync(null, "Tea", page: 1, pageSize: 10, sort: null, isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Category.Should().Be("Tea");
    }

    [Fact]
    public async Task GetProductsAsync_WithSort_SortsCorrectly()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "A", 5m));
        repository.AddProduct(CreateProduct(2, "B", 2m));
        var service = new ProductService(repository);

        var result = await service.GetProductsAsync(null, null, page: 1, pageSize: 10, sort: "priceAsc", isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Items.Select(i => i.Price).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetProductAsync_Exists_ReturnsProduct()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Tea", 1m));
        var service = new ProductService(repository);

        var result = await service.GetProductAsync(1, isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetProductAsync_NotExists_ReturnsNotFound()
    {
        var service = new ProductService(new FakeProductRepository());

        var result = await service.GetProductAsync(999, isAdmin: false);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task GetProductAsync_IncludesImages()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Tea", 1m));
        repository.AddImage(1, "https://example.com/1.jpg");
        repository.AddImage(1, "https://example.com/2.jpg");
        var service = new ProductService(repository);

        var result = await service.GetProductAsync(1, isAdmin: false);

        result.Success.Should().BeTrue();
        result.Value!.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateProductAsync_ValidData_CreatesProduct()
    {
        var repository = new FakeProductRepository();
        var service = new ProductService(repository);

        var result = await service.CreateProductAsync(new CreateProductRequest
        {
            Name = "New",
            Description = "Desc",
            Price = 4m,
            Cost = 1m,
            StockAmount = 3,
            CategoryId = 1
        });

        result.Success.Should().BeTrue();
        repository.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidData_UpdatesProduct()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Old", 1m));
        var service = new ProductService(repository);

        var result = await service.UpdateProductAsync(1, new UpdateProductRequest
        {
            Name = "Updated",
            Price = 2m
        });

        result.Success.Should().BeTrue();
        repository.Products.First().StrName.Should().Be("Updated");
        repository.Products.First().DecPrice.Should().Be(2m);
    }

    [Fact]
    public async Task UpdateProductAsync_NotFound_ReturnsError()
    {
        var service = new ProductService(new FakeProductRepository());

        var result = await service.UpdateProductAsync(999, new UpdateProductRequest { Name = "Nope" });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("NotFound");
    }

    [Fact]
    public async Task DeleteProductAsync_Exists_DeletesProduct()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Delete", 1m));
        repository.AddImage(1, "https://example.com/delete.jpg");
        var service = new ProductService(repository);

        var result = await service.DeleteProductAsync(1);

        result.Success.Should().BeTrue();
        repository.Products.Should().BeEmpty();
        repository.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task AddProductImageAsync_AddsImage()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Tea", 1m));
        var service = new ProductService(repository);

        var result = await service.AddProductImageAsync(1, new AddProductImageRequest { Url = "https://example.com/a.jpg" });

        result.Success.Should().BeTrue();
        repository.Images.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteProductImageAsync_DeletesImage()
    {
        var repository = new FakeProductRepository();
        repository.AddProduct(CreateProduct(1, "Tea", 1m));
        var image = repository.AddImage(1, "https://example.com/a.jpg");
        var service = new ProductService(repository);

        var result = await service.DeleteProductImageAsync(1, image.IntProductImageId);

        result.Success.Should().BeTrue();
        repository.Images.Should().BeEmpty();
    }

    private static Tproduct CreateProduct(int id, string name, decimal price, string? category = null)
    {
        return new Tproduct
        {
            IntProductId = id,
            StrName = name,
            strDescription = name,
            DecPrice = price,
            IntStockAmount = 5,
            IntDrinkCategory = category == null ? null : new TdrinkCategory { StrName = category }
        };
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly List<Tproduct> _products = new();
        private readonly List<TproductImage> _images = new();
        private int _nextProductId = 1;
        private int _nextImageId = 1;

        public IReadOnlyList<Tproduct> Products => _products;
        public IReadOnlyList<TproductImage> Images => _images;

        public void AddProduct(Tproduct product)
        {
            if (product.IntProductId == 0)
            {
                product.IntProductId = _nextProductId++;
            }

            _products.Add(product);
        }

        public TproductImage AddImage(int productId, string url)
        {
            var image = new TproductImage
            {
                IntProductImageId = _nextImageId++,
                IntProductId = productId,
                StrProductImageUrl = url
            };

            _images.Add(image);
            var product = _products.FirstOrDefault(p => p.IntProductId == productId);
            if (product != null)
            {
                product.TproductImages.Add(image);
            }

            return image;
        }

        public Task<Tproduct?> FindByIdAsync(int productId, bool track = true)
            => Task.FromResult(_products.FirstOrDefault(p => p.IntProductId == productId));

        public Task<IReadOnlyList<Tproduct>> GetByIdsAsync(IEnumerable<int> productIds, bool track = true)
        {
            var ids = productIds.ToHashSet();
            IReadOnlyList<Tproduct> products = _products.Where(p => ids.Contains(p.IntProductId)).ToList();
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
            IEnumerable<Tproduct> products = _products;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                products = products.Where(p =>
                    p.StrName.Contains(term) || p.strDescription.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var categoryTerm = category.Trim();
                products = products.Where(p =>
                    p.IntDrinkCategory != null &&
                    p.IntDrinkCategory.StrName == categoryTerm);
            }

            var total = products.Count();

            products = sort switch
            {
                "priceAsc" => products.OrderBy(p => p.DecPrice).ThenBy(p => p.IntProductId),
                "priceDesc" => products.OrderByDescending(p => p.DecPrice).ThenBy(p => p.IntProductId),
                _ => products.OrderBy(p => p.IntProductId)
            };

            products = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return Task.FromResult(((IReadOnlyList<Tproduct>)products.ToList(), total));
        }

        public Task<Tproduct?> FindByIdWithImagesAsync(int productId, bool track = false)
        {
            var product = _products.FirstOrDefault(p => p.IntProductId == productId);
            if (product != null)
            {
                product.TproductImages = _images.Where(i => i.IntProductId == productId).ToList();
            }

            return Task.FromResult(product);
        }

        public Task<bool> ExistsAsync(int productId)
            => Task.FromResult(_products.Any(p => p.IntProductId == productId));

        public Task<TproductImage?> FindImageAsync(int productId, int imageId)
            => Task.FromResult(_images.FirstOrDefault(i => i.IntProductId == productId && i.IntProductImageId == imageId));

        public Task AddAsync(Tproduct product)
        {
            AddProduct(product);
            return Task.CompletedTask;
        }

        public Task AddImageAsync(TproductImage image)
        {
            if (image.IntProductImageId == 0)
            {
                image.IntProductImageId = _nextImageId++;
            }

            _images.Add(image);
            var product = _products.FirstOrDefault(p => p.IntProductId == image.IntProductId);
            if (product != null)
            {
                product.TproductImages.Add(image);
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(Tproduct product)
        {
            _products.Remove(product);
            _images.RemoveAll(i => i.IntProductId == product.IntProductId);
            return Task.CompletedTask;
        }

        public Task RemoveImageAsync(TproductImage image)
        {
            _images.Remove(image);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
            => Task.CompletedTask;
    }
}
