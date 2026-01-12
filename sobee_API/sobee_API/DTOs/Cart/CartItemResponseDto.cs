namespace sobee_API.DTOs.Cart
{
    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        public DateTime? Added { get; set; }
        public CartProductDto? Product { get; set; }
        public decimal LineTotal { get; set; }
    }
}
