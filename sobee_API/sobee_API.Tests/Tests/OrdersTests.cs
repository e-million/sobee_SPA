using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class OrdersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OrdersTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Checkout_AsGuest_ReturnsOrderAndClearsCart()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 2 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = "123 Test Lane", paymentMethodId = 1 })
        };
        TestSessionHeaders.AddSessionHeaders(checkoutRequest, sessionId, sessionSecret);

        var checkoutResponse = await client.SendAsync(checkoutRequest);

        checkoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var checkoutJson = await TestJson.ReadJsonAsync(checkoutResponse);
        checkoutJson.RootElement.TryGetProperty("orderId", out var orderId).Should().BeTrue();
        orderId.GetInt32().Should().BeGreaterThan(0);
        checkoutJson.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().BeGreaterThan(0);

        using var cartRequest = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        TestSessionHeaders.AddSessionHeaders(cartRequest, sessionId, sessionSecret);

        var cartResponse = await client.SendAsync(cartRequest);

        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var cartJson = await TestJson.ReadJsonAsync(cartResponse);
        var cartItems = cartJson.RootElement.GetProperty("items");
        cartItems.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetOrder_NotFound_ReturnsApiErrorResponse()
    {
        var client = _factory.CreateClient();

        var sessionResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(sessionResponse);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/9999999");
        TestSessionHeaders.AddSessionHeaders(request, sessionId, sessionSecret);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("error").GetString().Should().Be("Order not found.");
        json.RootElement.GetProperty("code").GetString().Should().Be("NotFound");
        json.RootElement.GetProperty("details").GetProperty("orderId").GetInt32().Should().Be(9999999);
    }
}
