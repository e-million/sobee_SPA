using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class Phase0ProductsTests : IClassFixture<TestWebApplicationFactory>
{
    private const int CategoryId = 1;
    private const int ProductInStockId = 1;
    private const string CategoryName = "Tea";
    private const string AdminId = "admin-1";
    private const string UserId = "user-1";

    private readonly TestWebApplicationFactory _factory;

    public Phase0ProductsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PROD_001_ListProducts_Paging()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("page").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("pageSize").GetInt32().Should().Be(2);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeLessThanOrEqualTo(2);
        json.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterOrEqualTo(items.GetArrayLength());
    }

    [Fact]
    public async Task PROD_002_ListProducts_Search()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products?q=Low");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("name").GetString().Should().Contain("Low");
        }
    }

    [Fact]
    public async Task PROD_003_ListProducts_CategoryFilter()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/products?category={CategoryName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("category").GetString().Should().Be(CategoryName);
        }
    }

    [Fact]
    public async Task PROD_004_ListProducts_SortPriceAsc()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products?sort=priceAsc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterOrEqualTo(2);
        var prices = items.EnumerateArray().Select(i => i.GetProperty("price").GetDecimal()).ToArray();
        prices.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task PROD_005_ListProducts_AdminSeesCostStock()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/products", AdminId, "Admin");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        var product = items.EnumerateArray().First(i => i.GetProperty("id").GetInt32() == ProductInStockId);
        product.GetProperty("cost").GetDecimal().Should().BeGreaterThan(0);
        product.GetProperty("stockAmount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PROD_006_ListProducts_NonAdminHidesCostStock()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/products", UserId, "User");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        var product = items.EnumerateArray().First(i => i.GetProperty("id").GetInt32() == ProductInStockId);
        product.TryGetProperty("cost", out var cost).Should().BeTrue();
        cost.ValueKind.Should().Be(JsonValueKind.Null);
        product.TryGetProperty("stockAmount", out var stock).Should().BeTrue();
        stock.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PROD_007_GetProduct_NotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("NotFound");
    }

    [Fact]
    public async Task PROD_008_CreateProduct_AdminOnly()
    {
        var client = _factory.CreateClient();

        using var nonAdmin = CreateAuthRequest(HttpMethod.Post, "/api/products", UserId, "User", new
        {
            name = "Nope",
            description = "Nope",
            price = 1.5m,
            cost = 0.5m,
            stockAmount = 5,
            categoryId = CategoryId
        });
        var nonAdminResponse = await client.SendAsync(nonAdmin);
        nonAdminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var createResponse = await CreateProductAsync(client, "Admin Created");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PROD_009_UpdateProduct_Partial_AdminOnly()
    {
        var client = _factory.CreateClient();

        var productId = await CreateProductIdAsync(client, "Update Product");

        using var nonAdmin = CreateAuthRequest(HttpMethod.Put, $"/api/products/{productId}", UserId, "User", new { name = "Nope" });
        var nonAdminResponse = await client.SendAsync(nonAdmin);
        nonAdminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var update = CreateAuthRequest(HttpMethod.Put, $"/api/products/{productId}", AdminId, "Admin", new { name = "Updated Name" });
        var response = await client.SendAsync(update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("name").GetString().Should().Be("Updated Name");
    }

    [Fact]
    public async Task PROD_010_DeleteProduct_AdminOnly()
    {
        var client = _factory.CreateClient();

        var productId = await CreateProductIdAsync(client, "Delete Product");

        using var nonAdmin = CreateAuthRequest(HttpMethod.Delete, $"/api/products/{productId}", UserId, "User");
        var nonAdminResponse = await client.SendAsync(nonAdmin);
        nonAdminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var delete = CreateAuthRequest(HttpMethod.Delete, $"/api/products/{productId}", AdminId, "Admin");
        var response = await client.SendAsync(delete);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("message").GetString().Should().Be("Product deleted.");
    }

    [Fact]
    public async Task PROD_011_AddProductImage_AdminOnly()
    {
        var client = _factory.CreateClient();

        var productId = await CreateProductIdAsync(client, "Image Product");

        using var nonAdmin = CreateAuthRequest(HttpMethod.Post, $"/api/products/{productId}/images", UserId, "User", new { url = "https://example.com/na.jpg" });
        var nonAdminResponse = await client.SendAsync(nonAdmin);
        nonAdminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var add = CreateAuthRequest(HttpMethod.Post, $"/api/products/{productId}/images", AdminId, "Admin", new { url = "https://example.com/img.jpg" });
        var response = await client.SendAsync(add);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("productId").GetInt32().Should().Be(productId);
        json.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PROD_012_DeleteProductImage_AdminOnly()
    {
        var client = _factory.CreateClient();

        var productId = await CreateProductIdAsync(client, "Delete Image Product");

        using var add = CreateAuthRequest(HttpMethod.Post, $"/api/products/{productId}/images", AdminId, "Admin", new { url = "https://example.com/img-delete.jpg" });
        var addResponse = await client.SendAsync(add);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var addJson = await TestJson.ReadJsonAsync(addResponse);
        var imageId = addJson.RootElement.GetProperty("id").GetInt32();

        using var nonAdmin = CreateAuthRequest(HttpMethod.Delete, $"/api/products/{productId}/images/{imageId}", UserId, "User");
        var nonAdminResponse = await client.SendAsync(nonAdmin);
        nonAdminResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var delete = CreateAuthRequest(HttpMethod.Delete, $"/api/products/{productId}/images/{imageId}", AdminId, "Admin");
        var response = await client.SendAsync(delete);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("message").GetString().Should().Be("Image deleted.");
    }

    private static HttpRequestMessage CreateAuthRequest(HttpMethod method, string url, string userId, string roles, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RolesHeader, roles);
        return request;
    }

    private static async Task<HttpResponseMessage> CreateProductAsync(HttpClient client, string name)
    {
        using var request = CreateAuthRequest(HttpMethod.Post, "/api/products", AdminId, "Admin", new
        {
            name,
            description = "Created in test",
            price = 4.25m,
            cost = 1.5m,
            stockAmount = 5,
            categoryId = CategoryId
        });

        return await client.SendAsync(request);
    }

    private static async Task<int> CreateProductIdAsync(HttpClient client, string name)
    {
        var response = await CreateProductAsync(client, name + " " + Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = await TestJson.ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }
}
