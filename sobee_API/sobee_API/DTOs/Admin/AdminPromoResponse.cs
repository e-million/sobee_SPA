namespace sobee_API.DTOs.Admin
{
    public sealed class AdminPromoResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int UsageCount { get; set; }
        public bool IsExpired { get; set; }
    }
}
