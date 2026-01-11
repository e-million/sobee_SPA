namespace sobee_API.DTOs.Cart
{
    public class AddCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
