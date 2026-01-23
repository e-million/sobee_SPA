using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sobee.Domain.Identity;
using Xunit;

namespace sobee_API.Tests;

public class Phase0SmokeTests : IClassFixture<TestWebApplicationFactory>
{
    private const int ProductId = 1;
    private const string AdminId = "admin-1";
    private const string UserId = "user-1";
    private const string UserEmail = "user-1@example.com";

    private readonly TestWebApplicationFactory _factory;

    public Phase0SmokeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SMOKE_001_Auth_Register_HappyPath()
    {
        var client = _factory.CreateClient();

        var email = $"smoke-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "test123",
            firstName = "Smoke",
            lastName = "Test",
            billingAddress = "123 Test Lane",
            shippingAddress = "123 Test Lane"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("email").GetString().Should().Be(email);
        json.RootElement.GetProperty("message").GetString().Should().Contain("User created");
    }

    [Fact]
    public async Task SMOKE_002_Users_Profile_HappyPath()
    {
        await EnsureIdentityUserAsync(UserId, UserEmail);

        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/users/profile", UserId, "User", UserEmail);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("email").GetString().Should().Be(UserEmail);
        json.RootElement.GetProperty("firstName").GetString().Should().Be("Smoke");
        json.RootElement.GetProperty("lastName").GetString().Should().Be("User");
    }

    [Fact]
    public async Task SMOKE_003_Me_HappyPath()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/me", UserId, "User", "me@example.com");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("email").GetString().Should().Be("me@example.com");
        json.RootElement.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).Should().Contain("User");
    }

    [Fact]
    public async Task SMOKE_004_PaymentMethods_HappyPath()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/paymentmethods", UserId, "User", UserEmail);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        json.RootElement.GetArrayLength().Should().BeGreaterThan(0);
        json.RootElement[0].TryGetProperty("paymentMethodId", out _).Should().BeTrue();
        json.RootElement[0].TryGetProperty("description", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SMOKE_005_Reviews_ByProduct_HappyPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/reviews/product/{ProductId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("productId").GetInt32().Should().Be(ProductId);
        json.RootElement.TryGetProperty("summary", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("reviews", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SMOKE_006_Favorites_List_HappyPath()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/favorites", UserId, "User", UserEmail);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("userId").GetString().Should().Be(UserId);
        json.RootElement.TryGetProperty("favorites", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SMOKE_007_AdminAnalytics_InventorySummary_HappyPath()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/admin/analytics/inventory/summary", AdminId, "Admin");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("totalProducts").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.TryGetProperty("inStockCount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SMOKE_008_AdminPromos_List_HappyPath()
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/admin/promos", AdminId, "Admin");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SMOKE_009_AdminUsers_List_HappyPath()
    {
        await EnsureIdentityUserAsync(UserId, UserEmail);

        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, "/api/admin/users", AdminId, "Admin");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("items").GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    private static HttpRequestMessage CreateAuthRequest(HttpMethod method, string url, string userId, string roles, string? email = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RolesHeader, roles);
        if (!string.IsNullOrWhiteSpace(email))
        {
            request.Headers.Add(TestAuthHandler.EmailHeader, email);
        }

        return request;
    }

    private async Task EnsureIdentityUserAsync(string userId, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByIdAsync(userId);
        if (existing != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            strFirstName = "Smoke",
            strLastName = "User",
            strBillingAddress = "123 Test Lane",
            strShippingAddress = "123 Test Lane",
            CreatedDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "test123");
        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors.Select(e => e.Description)));
    }
}
