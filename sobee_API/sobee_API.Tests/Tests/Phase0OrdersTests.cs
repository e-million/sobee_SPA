using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sobee.Domain.Data;
using Xunit;

namespace sobee_API.Tests;

public class Phase0OrdersTests : IClassFixture<TestWebApplicationFactory>
{
    private const int ProductInStockId = 1;
    private const int ProductLowStockId = 2;
    private const int PaymentMethodId = 1;
    private const string UserId = "user-123";
    private const string AdminId = "admin-1";

    private readonly TestWebApplicationFactory _factory;

    public Phase0OrdersTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ORDER_001_Checkout_EmptyCart_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await CreateGuestCartAsync(client);

        using var checkout = CreateSessionRequest(HttpMethod.Post, "/api/orders/checkout", sessionId, sessionSecret, new
        {
            shippingAddress = "123 Test Lane",
            billingAddress = "123 Test Lane",
            paymentMethodId = PaymentMethodId
        });

        var response = await client.SendAsync(checkout);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("ValidationError");
    }

    [Fact]
    public async Task ORDER_002_Checkout_Success_CreatesOrder_AndClearsCart()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 2);

        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);
        orderId.Should().BeGreaterThan(0);

        using var cartRequest = CreateSessionRequest(HttpMethod.Get, "/api/cart", sessionId, sessionSecret);
        var cartResponse = await client.SendAsync(cartRequest);

        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cartJson = await TestJson.ReadJsonAsync(cartResponse);
        cartJson.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ORDER_003_Checkout_InsufficientStock_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductLowStockId, 1);

        using (var scope_ = _factory.Services.CreateScope())
        {
            var database = scope_.ServiceProvider.GetRequiredService<SobeecoredbContext>();
            var product_ = await database.Tproducts.FirstAsync(p => p.IntProductId == ProductLowStockId);
            product_.IntStockAmount = 0;
            await database.SaveChangesAsync();
        }

        using var checkout = CreateSessionRequest(HttpMethod.Post, "/api/orders/checkout", sessionId, sessionSecret, new
        {
            shippingAddress = "123 Test Lane",
            billingAddress = "123 Test Lane",
            paymentMethodId = PaymentMethodId
        });

        var response = await client.SendAsync(checkout);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("InsufficientStock");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SobeecoredbContext>();
        var product = await db.Tproducts.AsNoTracking().FirstAsync(p => p.IntProductId == ProductLowStockId);
        product.IntStockAmount.Should().Be(0);
    }

    [Fact]
    public async Task ORDER_004_GetOrder_Owner_ReturnsOrder()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        using var request = CreateSessionRequest(HttpMethod.Get, $"/api/orders/{orderId}", sessionId, sessionSecret);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("orderId").GetInt32().Should().Be(orderId);
    }

    [Fact]
    public async Task ORDER_005_GetOrder_NonOwner_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        var (otherSessionId, otherSessionSecret) = await CreateGuestCartAsync(client);
        using var request = CreateSessionRequest(HttpMethod.Get, $"/api/orders/{orderId}", otherSessionId, otherSessionSecret);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("NotFound");
    }

    [Fact]
    public async Task ORDER_006_GetMyOrders_PaginationHeaders()
    {
        var client = _factory.CreateClient();

        await CreateUserOrderAsync(client, UserId);

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/orders/my?page=1&pageSize=20", UserId, "User");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Total-Count").Should().BeTrue();
        response.Headers.Contains("X-Page").Should().BeTrue();
        response.Headers.Contains("X-Page-Size").Should().BeTrue();

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ORDER_007_CancelOrder_InvalidStatus_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        (await PatchStatusAsync(client, orderId, "Paid")).Should().Be(HttpStatusCode.OK);
        (await PatchStatusAsync(client, orderId, "Processing")).Should().Be(HttpStatusCode.OK);
        (await PatchStatusAsync(client, orderId, "Shipped")).Should().Be(HttpStatusCode.OK);

        using var cancel = CreateSessionRequest(HttpMethod.Post, $"/api/orders/{orderId}/cancel", sessionId, sessionSecret);
        var response = await client.SendAsync(cancel);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("InvalidStatusTransition");
    }

    [Fact]
    public async Task ORDER_008_PayOrder_InvalidMethod_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        using var pay = CreateSessionRequest(HttpMethod.Post, $"/api/orders/{orderId}/pay", sessionId, sessionSecret, new { paymentMethodId = 999 });
        var response = await client.SendAsync(pay);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("NotFound");
    }

    [Fact]
    public async Task ORDER_009_PayOrder_Valid_TransitionsToPaid()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        using var pay = CreateSessionRequest(HttpMethod.Post, $"/api/orders/{orderId}/pay", sessionId, sessionSecret, new { paymentMethodId = PaymentMethodId });
        var response = await client.SendAsync(pay);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("orderStatus").GetString().Should().Be("Paid");
    }

    [Fact]
    public async Task ORDER_010_Admin_UpdateStatus_Valid_SetsTimestamps()
    {
        var client = _factory.CreateClient();

        var (sessionId, sessionSecret) = await AddCartItemAsync(client, ProductInStockId, 1);
        var orderId = await CheckoutAsync(client, sessionId, sessionSecret);

        (await PatchStatusAsync(client, orderId, "Paid")).Should().Be(HttpStatusCode.OK);
        (await PatchStatusAsync(client, orderId, "Processing")).Should().Be(HttpStatusCode.OK);

        using var patch = CreateAuthRequest(HttpMethod.Patch, $"/api/orders/{orderId}/status", AdminId, "Admin", new { status = "Shipped" });
        var response = await client.SendAsync(patch);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("orderStatus").GetString().Should().Be("Shipped");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SobeecoredbContext>();
        var order = await db.Torders.AsNoTracking().FirstAsync(o => o.IntOrderId == orderId);
        order.DtmShippedDate.Should().NotBeNull();
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

    private static async Task<(string sessionId, string sessionSecret)> CreateGuestCartAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/cart");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return TestSessionHeaders.GetSessionHeaders(response);
    }

    private static async Task<(string sessionId, string sessionSecret)> AddCartItemAsync(HttpClient client, int productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/cart/items", new { productId, quantity });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return TestSessionHeaders.GetSessionHeaders(response);
    }

    private static async Task<int> CheckoutAsync(HttpClient client, string sessionId, string sessionSecret)
    {
        using var checkout = CreateSessionRequest(HttpMethod.Post, "/api/orders/checkout", sessionId, sessionSecret, new
        {
            shippingAddress = "123 Test Lane",
            billingAddress = "123 Test Lane",
            paymentMethodId = PaymentMethodId
        });

        var response = await client.SendAsync(checkout);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        return json.RootElement.GetProperty("orderId").GetInt32();
    }

    private static async Task<HttpStatusCode> PatchStatusAsync(HttpClient client, int orderId, string status)
    {
        using var patch = CreateAuthRequest(HttpMethod.Patch, $"/api/orders/{orderId}/status", AdminId, "Admin", new { status });
        var response = await client.SendAsync(patch);
        return response.StatusCode;
    }

    private static async Task<int> CreateUserOrderAsync(HttpClient client, string userId)
    {
        using var getCart = CreateAuthRequest(HttpMethod.Get, "/api/cart", userId, "User");
        var cartResponse = await client.SendAsync(getCart);
        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var addItem = CreateAuthRequest(HttpMethod.Post, "/api/cart/items", userId, "User", new { productId = ProductInStockId, quantity = 1 });
        var addResponse = await client.SendAsync(addItem);
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var checkout = CreateAuthRequest(HttpMethod.Post, "/api/orders/checkout", userId, "User", new
        {
            shippingAddress = "123 Test Lane",
            billingAddress = "123 Test Lane",
            paymentMethodId = PaymentMethodId
        });

        var response = await client.SendAsync(checkout);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        return json.RootElement.GetProperty("orderId").GetInt32();
    }
}
