namespace sobee_API.DTOs.Admin
{
    public sealed class UpdatePromoRequest
    {
        public string? Code { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
