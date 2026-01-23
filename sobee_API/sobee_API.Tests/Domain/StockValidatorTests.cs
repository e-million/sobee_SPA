using FluentAssertions;
using sobee_API.Domain;
using Xunit;

namespace sobee_API.Tests.Domain;

public class StockValidatorTests
{
    [Fact]
    public void Validate_SufficientStock_ReturnsValid()
    {
        var result = StockValidator.Validate(10, 3);

        result.IsValid.Should().BeTrue();
        result.AvailableStock.Should().Be(10);
        result.Requested.Should().Be(3);
    }

    [Fact]
    public void Validate_ExactStock_ReturnsValid()
    {
        var result = StockValidator.Validate(5, 5);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InsufficientStock_ReturnsInvalidWithAvailable()
    {
        var result = StockValidator.Validate(2, 4);

        result.IsValid.Should().BeFalse();
        result.AvailableStock.Should().Be(2);
        result.Requested.Should().Be(4);
    }

    [Fact]
    public void Validate_ZeroAvailable_ReturnsInvalid()
    {
        var result = StockValidator.Validate(0, 1);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ZeroRequested_ReturnsValid()
    {
        var result = StockValidator.Validate(5, 0);

        result.IsValid.Should().BeTrue();
    }
}
