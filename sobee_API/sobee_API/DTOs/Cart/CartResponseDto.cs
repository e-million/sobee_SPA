namespace sobee_API.DTOs.Cart
{
    public class CartResponseDto
    {
        public int CartId { get; set; }
        public string Owner { get; set; } = "guest";
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new();
        public CartPromoDto? Promo { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxRate { get; set; }
        public decimal Total { get; set; }
    }
}
