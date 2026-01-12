using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class ProductsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProductsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedItems()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.TryGetProperty("items", out var items).Should().BeTrue();
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().BeGreaterThan(0);

        var firstItem = items[0];
        firstItem.TryGetProperty("id", out var id).Should().BeTrue();
        id.GetInt32().Should().BeGreaterThan(0);
        firstItem.TryGetProperty("name", out var name).Should().BeTrue();
        name.GetString().Should().NotBeNullOrWhiteSpace();
        firstItem.TryGetProperty("price", out var price).Should().BeTrue();
        price.GetDecimal().Should().BeGreaterThan(0);
    }
}
