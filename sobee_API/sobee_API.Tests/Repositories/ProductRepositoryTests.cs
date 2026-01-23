using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using Sobee.Domain.Repositories;
using Xunit;

namespace sobee_API.Tests.Repositories;

public class ProductRepositoryTests
{
    [Fact]
    public async Task GetProductsAsync_SearchFiltersCorrectly()
    {
        using var context = new SqliteTestContext();
        context.AddProduct(CreateProduct(1, "Low Sugar", 2m));
        context.AddProduct(CreateProduct(2, "High Sugar", 3m));

        var (items, total) = await context.Repository.GetProductsAsync("Low", null, page: 1, pageSize: 10, sort: null);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].StrName.Should().Contain("Low");
    }

    [Fact]
    public async Task GetProductsAsync_CategoryFiltersCorrectly()
    {
        using var context = new SqliteTestContext();
        var tea = context.AddCategory("Tea");
        var coffee = context.AddCategory("Coffee");
        context.AddProduct(CreateProduct(1, "Tea Item", 2m, tea));
        context.AddProduct(CreateProduct(2, "Coffee Item", 3m, coffee));

        var (items, total) = await context.Repository.GetProductsAsync(null, "Tea", page: 1, pageSize: 10, sort: null);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].IntDrinkCategory!.StrName.Should().Be("Tea");
    }

    [Fact]
    public async Task GetProductsAsync_SortsPriceAsc()
    {
        using var context = new SqliteTestContext();
        context.AddProduct(CreateProduct(1, "A", 5m));
        context.AddProduct(CreateProduct(2, "B", 2m));

        var (items, _) = await context.Repository.GetProductsAsync(null, null, page: 1, pageSize: 10, sort: "priceAsc");

        items.Select(p => p.DecPrice).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task FindByIdWithImagesAsync_IncludesImages()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, "Tea", 2m));
        context.AddImage(product.IntProductId, "https://example.com/1.jpg");

        var result = await context.Repository.FindByIdWithImagesAsync(product.IntProductId);

        result.Should().NotBeNull();
        result!.TproductImages.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddImageAsync_AddsImage()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, "Tea", 2m));

        await context.Repository.AddImageAsync(new TproductImage
        {
            IntProductId = product.IntProductId,
            StrProductImageUrl = "https://example.com/1.jpg"
        });
        await context.Repository.SaveChangesAsync();

        var count = await context.DbContext.TproductImages.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task FindImageAsync_ReturnsImage()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, "Tea", 2m));
        var image = context.AddImage(product.IntProductId, "https://example.com/1.jpg");

        var result = await context.Repository.FindImageAsync(product.IntProductId, image.IntProductImageId);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue()
    {
        using var context = new SqliteTestContext();
        var product = context.AddProduct(CreateProduct(1, "Tea", 2m));

        var result = await context.Repository.ExistsAsync(product.IntProductId);

        result.Should().BeTrue();
    }

    private static Tproduct CreateProduct(int id, string name, decimal price, TdrinkCategory? category = null)
        => new()
        {
            IntProductId = id,
            StrName = name,
            strDescription = name,
            DecPrice = price,
            IntStockAmount = 5,
            IntDrinkCategoryId = category?.IntDrinkCategoryId,
            IntDrinkCategory = category
        };

    private sealed class SqliteTestContext : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SobeecoredbContext DbContext { get; }
        public ProductRepository Repository { get; }

        public SqliteTestContext()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SobeecoredbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new SobeecoredbContext(options);
            DbContext.Database.EnsureCreated();

            Repository = new ProductRepository(DbContext);
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Dispose();
        }

        public TdrinkCategory AddCategory(string name)
        {
            var category = new TdrinkCategory
            {
                StrName = name,
                StrDescription = name
            };

            DbContext.TdrinkCategories.Add(category);
            DbContext.SaveChanges();
            return category;
        }

        public Tproduct AddProduct(Tproduct product)
        {
            DbContext.Tproducts.Add(product);
            DbContext.SaveChanges();
            return product;
        }

        public TproductImage AddImage(int productId, string url)
        {
            var image = new TproductImage
            {
                IntProductId = productId,
                StrProductImageUrl = url
            };

            DbContext.TproductImages.Add(image);
            DbContext.SaveChanges();
            return image;
        }
    }
}
