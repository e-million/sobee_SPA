using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace sobee_API.Tests;

public class ValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddItem_WithInvalidQuantity_ReturnsValidationApiError()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("error").GetString().Should().Be("Validation failed.");
        json.RootElement.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        var errors = json.RootElement.GetProperty("details").GetProperty("errors");
        errors.TryGetProperty("quantity", out var quantityErrors).Should().BeTrue();
        quantityErrors.ValueKind.Should().Be(JsonValueKind.Array);
        quantityErrors[0].GetString().Should().Contain("Quantity must be greater than 0.");
    }

    [Fact]
    public async Task AddItem_WithInvalidJson_ReturnsBodyErrorKey()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/items")
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var json = await TestJson.ReadJsonAsync(response);
        json.RootElement.GetProperty("error").GetString().Should().Be("Validation failed.");
        json.RootElement.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        var errors = json.RootElement.GetProperty("details").GetProperty("errors");
        errors.TryGetProperty("body", out var bodyErrors).Should().BeTrue();
        bodyErrors.ValueKind.Should().Be(JsonValueKind.Array);
    }

    // No request DTOs contain list/array properties, so indexer key normalization cannot be asserted here.
}
