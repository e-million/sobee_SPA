using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class CartTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CartTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddItem_AsGuest_ReturnsSessionHeadersAndCartDto()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains(TestSessionHeaders.SessionIdHeader).Should().BeTrue();
        response.Headers.Contains(TestSessionHeaders.SessionSecretHeader).Should().BeTrue();

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.TryGetProperty("cartId", out var cartId).Should().BeTrue();
        cartId.GetInt32().Should().BeGreaterThan(0);
        var items = json.RootElement.GetProperty("items");
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().BeGreaterThan(0);
        items[0].GetProperty("productId").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetCart_WithSessionHeaders_PersistsItems()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 1 });
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(addResponse);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        TestSessionHeaders.AddSessionHeaders(request, sessionId, sessionSecret);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await TestJson.ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items");
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApplyAndRemovePromo_ReturnsPromoDtos()
    {
        var client = _factory.CreateClient();

        var cartResponse = await client.GetAsync("/api/cart");
        var (sessionId, sessionSecret) = TestSessionHeaders.GetSessionHeaders(cartResponse);

        using var applyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/cart/promo/apply")
        {
            Content = JsonContent.Create(new { promoCode = "PROMO10" })
        };
        TestSessionHeaders.AddSessionHeaders(applyRequest, sessionId, sessionSecret);

        var applyResponse = await client.SendAsync(applyRequest);

        applyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var applyJson = await TestJson.ReadJsonAsync(applyResponse);
        applyJson.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        applyJson.RootElement.GetProperty("promoCode").GetString().Should().Be("PROMO10");
        applyJson.RootElement.GetProperty("discountPercentage").GetDecimal().Should().BeGreaterThan(0);

        using var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/cart/promo");
        TestSessionHeaders.AddSessionHeaders(removeRequest, sessionId, sessionSecret);

        var removeResponse = await client.SendAsync(removeRequest);

        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var removeJson = await TestJson.ReadJsonAsync(removeResponse);
        removeJson.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
