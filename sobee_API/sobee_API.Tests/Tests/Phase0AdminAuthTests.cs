using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class Phase0AdminAuthTests : IClassFixture<TestWebApplicationFactory>
{
    private const string UserId = "user-1";

    private readonly TestWebApplicationFactory _factory;

    public Phase0AdminAuthTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/promos")]
    [InlineData("/api/admin/analytics/inventory/summary")]
    public async Task ADMIN_AUTH_001_AdminEndpoints_RequireAuthentication(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/promos")]
    [InlineData("/api/admin/analytics/inventory/summary")]
    public async Task ADMIN_AUTH_002_AdminEndpoints_RequireAdminRole(string url)
    {
        var client = _factory.CreateClient();

        using var request = CreateAuthRequest(HttpMethod.Get, url, UserId, "User");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static HttpRequestMessage CreateAuthRequest(HttpMethod method, string url, string userId, string roles)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RolesHeader, roles);
        return request;
    }
}
