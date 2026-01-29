using System;

namespace sobee_API.Domain;

public static class TaxCalculator
{
    public static decimal CalculateTax(decimal taxableAmount, decimal taxRate)
    {
        if (taxableAmount <= 0m || taxRate <= 0m)
        {
            return 0m;
        }

        return Math.Round(taxableAmount * taxRate, 2, MidpointRounding.AwayFromZero);
    }
}
