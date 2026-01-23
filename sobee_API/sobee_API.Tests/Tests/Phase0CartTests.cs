using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class Phase0CartTests : IClassFixture<TestWebApplicationFactory>
{
    private const int ProductInStockId = 1;
    private const int ProductLowStockId = 2;
    private const string PromoActiveCode = "PROMO10";
    private const string PromoExpiredCode = "EXPIRED10";
    private const string UserId = "user-1";

    private readonly TestWebApplicationFactory _factory;

    public Phase0CartTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CART_001_Guest_GetCart_CreatesSessionHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TestSessionHeaders.TryGetSessionHeaders(response, out _).Should().BeTrue();

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CART_002_Guest_AddItem_NewItem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductInStockId, quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TestSessionHeaders.TryGetSessionHeaders(response, out _).Should().BeTrue();

        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("productId").GetInt32().Should().Be(ProductInStockId);
        items[0].GetProperty("quantity").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CART_003_Guest_AddItem_Increments()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductInStockId, quantity = 1 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var secondAdd = CreateSessionRequest(HttpMethod.Post, "/api/cart/items", sessionId, sessionSecret, new { productId = ProductInStockId, quantity = 2 });
        var response = await client.SendAsync(secondAdd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("quantity").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task CART_004_AddItem_ExceedsStock_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductLowStockId, quantity = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task CART_005_UpdateItem_ToZero_Removes()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductInStockId, quantity = 1 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var addJson = await TestJson.ReadJsonAsync(addResponse);
        var cartItemId = addJson.RootElement.GetProperty("items")[0].GetProperty("cartItemId").GetInt32();

        using var update = CreateSessionRequest(HttpMethod.Put, $"/api/cart/items/{cartItemId}", sessionId, sessionSecret, new { quantity = 0 });
        var response = await client.SendAsync(update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task CART_006_UpdateItem_ExceedsStock_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductLowStockId, quantity = 1 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var addJson = await TestJson.ReadJsonAsync(addResponse);
        var cartItemId = addJson.RootElement.GetProperty("items")[0].GetProperty("cartItemId").GetInt32();

        using var update = CreateSessionRequest(HttpMethod.Put, $"/api/cart/items/{cartItemId}", sessionId, sessionSecret, new { quantity = 2 });
        var response = await client.SendAsync(update);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("InsufficientStock");
    }

    [Fact]
    public async Task CART_007_ApplyPromo_Valid_ReturnsDiscount()
    {
        var client = _factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(cartResponse);

        using var apply = CreateSessionRequest(HttpMethod.Post, "/api/cart/promo/apply", sessionId, sessionSecret, new { promoCode = PromoActiveCode });
        var response = await client.SendAsync(apply);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("promoCode").GetString().Should().Be(PromoActiveCode);
        json.RootElement.GetProperty("discountPercentage").GetDecimal().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CART_008_ApplyPromo_Expired_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(cartResponse);

        using var apply = CreateSessionRequest(HttpMethod.Post, "/api/cart/promo/apply", sessionId, sessionSecret, new { promoCode = PromoExpiredCode });
        var response = await client.SendAsync(apply);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("InvalidPromo");
    }

    [Fact]
    public async Task CART_009_ApplyPromo_Duplicate_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(cartResponse);

        using var apply1 = CreateSessionRequest(HttpMethod.Post, "/api/cart/promo/apply", sessionId, sessionSecret, new { promoCode = PromoActiveCode });
        var first = await client.SendAsync(apply1);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        using var apply2 = CreateSessionRequest(HttpMethod.Post, "/api/cart/promo/apply", sessionId, sessionSecret, new { promoCode = PromoActiveCode });
        var second = await client.SendAsync(apply2);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var json = await TestJson.ReadJsonAsync(second);
        json.RootElement.GetProperty("code").GetString().Should().Be("Conflict");
    }

    [Fact]
    public async Task CART_010_RemovePromo_None_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(cartResponse);

        using var remove = CreateSessionRequest(HttpMethod.Delete, "/api/cart/promo", sessionId, sessionSecret);
        var response = await client.SendAsync(remove);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("ValidationError");
    }

    [Fact]
    public async Task CART_011_Auth_GetCart_MergesGuestCart()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = ProductInStockId, quantity = 1 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var request = CreateSessionRequest(HttpMethod.Get, "/api/cart", sessionId, sessionSecret);
        AddAuthHeaders(request, UserId, "User");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("owner").GetString().Should().Be("user");
        json.RootElement.GetProperty("userId").GetString().Should().Be(UserId);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    private static HttpRequestMessage CreateSessionRequest(HttpMethod method, string url, string sessionId, string sessionSecret, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        TestSessionHeaders.AddSessionHeaders(request, sessionId, sessionSecret);
        return request;
    }

    private static void AddAuthHeaders(HttpRequestMessage request, string userId, string roles)
    {
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RolesHeader, roles);
    }
}
