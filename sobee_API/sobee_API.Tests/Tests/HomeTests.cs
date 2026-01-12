using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class HomeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HomeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ping_ReturnsStatusAndDatabaseFlag()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/home/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.TryGetProperty("db", out var db).Should().BeTrue();
        db.ValueKind.Should().Be(JsonValueKind.True).Or.Be(JsonValueKind.False);
    }
}
