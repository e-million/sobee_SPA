namespace sobee_API.DTOs.Admin
{
    public sealed class CreatePromoRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
