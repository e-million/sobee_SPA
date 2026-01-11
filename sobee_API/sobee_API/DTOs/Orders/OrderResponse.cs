namespace sobee_API.DTOs.Orders
{
    public sealed class OrderResponse
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? OrderStatus { get; set; }
        public string OwnerType { get; set; } = "guest";
        public string? UserId { get; set; }
        public string? GuestSessionId { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public decimal? SubtotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string? PromoCode { get; set; }
    }
}
