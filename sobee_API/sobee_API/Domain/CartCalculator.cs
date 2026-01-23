using System;
using System.Collections.Generic;

namespace sobee_API.Domain;

public static class CartCalculator
{
    public static decimal CalculateSubtotal(IEnumerable<CartLineItem> items)
    {
        decimal subtotal = 0m;

        foreach (var item in items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            subtotal += lineTotal;
        }

        return subtotal;
    }

    public static decimal CalculateTotal(decimal subtotal, decimal discount)
    {
        var total = subtotal - discount;
        return total < 0 ? 0m : total;
    }
}

public sealed record CartLineItem(int Quantity, decimal UnitPrice);
