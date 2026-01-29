using FluentAssertions;
using sobee_API.Domain;
using Xunit;

namespace sobee_API.Tests.Domain;

public class TaxCalculatorTests
{
    [Fact]
    public void CalculateTax_PositiveSubtotal_ReturnsRoundedTax()
    {
        var tax = TaxCalculator.CalculateTax(19.99m, 0.08m);

        tax.Should().Be(1.60m);
    }

    [Fact]
    public void CalculateTax_ZeroSubtotal_ReturnsZero()
    {
        var tax = TaxCalculator.CalculateTax(0m, 0.08m);

        tax.Should().Be(0m);
    }

    [Fact]
    public void CalculateTax_ZeroRate_ReturnsZero()
    {
        var tax = TaxCalculator.CalculateTax(100m, 0m);

        tax.Should().Be(0m);
    }

    [Fact]
    public void CalculateTax_NegativeSubtotal_ReturnsZero()
    {
        var tax = TaxCalculator.CalculateTax(-10m, 0.08m);

        tax.Should().Be(0m);
    }
}
