namespace sobee_API.Domain;

public static class PromoCalculator
{
    public static decimal CalculateDiscount(decimal subtotal, decimal discountPercentage)
    {
        if (subtotal <= 0 || discountPercentage <= 0)
        {
            return 0m;
        }

        var discount = subtotal * (discountPercentage / 100m);

        if (discount < 0)
        {
            return 0m;
        }

        return discount > subtotal ? subtotal : discount;
    }
}
