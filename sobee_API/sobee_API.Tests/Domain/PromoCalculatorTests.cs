using FluentAssertions;
using sobee_API.Domain;
using Xunit;

namespace sobee_API.Tests.Domain;

public class PromoCalculatorTests
{
    [Fact]
    public void CalculateDiscount_ZeroPercent_ReturnsZero()
    {
        var discount = PromoCalculator.CalculateDiscount(100m, 0m);

        discount.Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_TenPercent_ReturnsCorrectAmount()
    {
        var discount = PromoCalculator.CalculateDiscount(200m, 10m);

        discount.Should().Be(20m);
    }

    [Fact]
    public void CalculateDiscount_HundredPercent_ReturnsSubtotal()
    {
        var discount = PromoCalculator.CalculateDiscount(75m, 100m);

        discount.Should().Be(75m);
    }

    [Fact]
    public void CalculateDiscount_NegativeSubtotal_ReturnsZero()
    {
        var discount = PromoCalculator.CalculateDiscount(-10m, 10m);

        discount.Should().Be(0m);
    }

    [Fact]
    public void CalculateDiscount_NegativePercent_ReturnsZero()
    {
        var discount = PromoCalculator.CalculateDiscount(50m, -5m);

        discount.Should().Be(0m);
    }
}
