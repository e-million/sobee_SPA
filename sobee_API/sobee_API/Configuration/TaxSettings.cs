namespace sobee_API.Configuration;

public sealed class TaxSettings
{
    public decimal DefaultTaxRate { get; set; } = 0.08m;
    public bool TaxEnabled { get; set; } = true;
}
