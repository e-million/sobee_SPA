using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sobee.Domain.Data;
using Sobee.Domain.Entities.Products;
using System.Linq;
using Xunit;

namespace sobee_API.Tests;

public class GuestSessionTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public GuestSessionTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCart_IssuesSessionHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Session-Id", out var sessionIds));
        Assert.True(response.Headers.TryGetValues("X-Session-Secret", out var secrets));
        var sessionId = sessionIds.Single();
        var secret = secrets.Single();
        Assert.True(Guid.TryParse(sessionId, out _));
        Assert.False(string.IsNullOrWhiteSpace(secret));
    }

    [Fact]
    public async Task GetCart_WithValidatedSession_DoesNotRotateHeaders()
    {
        var client = _factory.CreateClient();

        var firstResponse = await client.GetAsync("/api/cart");
        var sessionId = firstResponse.Headers.GetValues("X-Session-Id").Single();
        var secret = firstResponse.Headers.GetValues("X-Session-Secret").Single();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Add("X-Session-Id", sessionId);
        request.Headers.Add("X-Session-Secret", secret);

        var secondResponse = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.False(secondResponse.Headers.Contains("X-Session-Id"));
        Assert.False(secondResponse.Headers.Contains("X-Session-Secret"));
    }

    [Fact]
    public async Task AddItem_WithSessionPair_ReturnsCart()
    {
        await SeedProductAsync();
        var client = _factory.CreateClient();

        var sessionResponse = await client.GetAsync("/api/cart");
        var sessionId = sessionResponse.Headers.GetValues("X-Session-Id").Single();
        var secret = sessionResponse.Headers.GetValues("X-Session-Secret").Single();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/items")
        {
            Content = JsonContent.Create(new { productId = 1, quantity = 1 })
        };
        request.Headers.Add("X-Session-Id", sessionId);
        request.Headers.Add("X-Session-Secret", secret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"productId\":1", body);
    }

    private async Task SeedProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SobeecoredbContext>();
        await db.Database.EnsureCreatedAsync();
        if (!db.Tproducts.Any())
        {
            db.Tproducts.Add(new Tproduct
            {
                IntProductId = 1,
                StrName = "Test Product",
                strDescription = "Test Description",
                DecPrice = 1.99m,
                StrStockAmount = "10"
            });
            await db.SaveChangesAsync();
        }
    }
}
