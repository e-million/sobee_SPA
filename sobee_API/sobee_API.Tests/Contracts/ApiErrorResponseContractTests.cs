using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using sobee_API.DTOs.Common;
using Xunit;

namespace sobee_API.Tests.Contracts;

public class ApiErrorResponseContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void ErrorResponse_WithCodeAndDetails_SerializesFields()
    {
        var response = new ApiErrorResponse("Bad request.", "ValidationError", new { id = 1, reason = "missing" });

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("error").GetString().Should().Be("Bad request.");
        root.GetProperty("code").GetString().Should().Be("ValidationError");
        root.TryGetProperty("details", out var details).Should().BeTrue();
        details.GetProperty("id").GetInt32().Should().Be(1);
        details.GetProperty("reason").GetString().Should().Be("missing");
    }

    [Fact]
    public void ErrorResponse_NullCodeAndDetails_OmitsFields()
    {
        var response = new ApiErrorResponse("Not found.");

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("error").GetString().Should().Be("Not found.");
        root.TryGetProperty("code", out _).Should().BeFalse();
        root.TryGetProperty("details", out _).Should().BeFalse();
    }

    [Fact]
    public void ValidationError_DetailsContainErrorsMap()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["body"] = new[] { "Required." }
        };

        var response = new ApiErrorResponse("Validation failed.", "VALIDATION_ERROR", new { errors });

        var json = JsonSerializer.Serialize(response, JsonOptions);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("error").GetString().Should().Be("Validation failed.");
        root.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        var bodyErrors = root.GetProperty("details")
            .GetProperty("errors")
            .GetProperty("body");

        bodyErrors.GetArrayLength().Should().Be(1);
        bodyErrors[0].GetString().Should().Be("Required.");
    }
}
