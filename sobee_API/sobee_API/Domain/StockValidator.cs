namespace sobee_API.Domain;

public static class StockValidator
{
    public static StockValidationResult Validate(int available, int requested)
    {
        var isValid = requested <= available;
        return new StockValidationResult(isValid, available, requested);
    }
}

public sealed record StockValidationResult(bool IsValid, int AvailableStock, int Requested);
