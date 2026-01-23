using System.Collections.Generic;
using FluentAssertions;
using sobee_API.Domain;
using Xunit;

namespace sobee_API.Tests.Domain;

public class CartCalculatorTests
{
    [Fact]
    public void CalculateSubtotal_EmptyItems_ReturnsZero()
    {
        var subtotal = CartCalculator.CalculateSubtotal(new List<CartLineItem>());

        subtotal.Should().Be(0m);
    }

    [Fact]
    public void CalculateSubtotal_SingleItem_ReturnsCorrectTotal()
    {
        var items = new List<CartLineItem>
        {
            new(2, 4.50m)
        };

        var subtotal = CartCalculator.CalculateSubtotal(items);

        subtotal.Should().Be(9.00m);
    }

    [Fact]
    public void CalculateSubtotal_MultipleItems_SumsCorrectly()
    {
        var items = new List<CartLineItem>
        {
            new(1, 3m),
            new(4, 2.25m),
            new(2, 1.10m)
        };

        var subtotal = CartCalculator.CalculateSubtotal(items);

        subtotal.Should().Be(3m + 9m + 2.2m);
    }

    [Fact]
    public void CalculateTotal_NoDiscount_ReturnsSubtotal()
    {
        var total = CartCalculator.CalculateTotal(25m, 0m);

        total.Should().Be(25m);
    }

    [Fact]
    public void CalculateTotal_WithDiscount_SubtractsDiscount()
    {
        var total = CartCalculator.CalculateTotal(25m, 5m);

        total.Should().Be(20m);
    }

    [Fact]
    public void CalculateTotal_DiscountExceedsSubtotal_ReturnsZero()
    {
        var total = CartCalculator.CalculateTotal(10m, 15m);

        total.Should().Be(0m);
    }
}
