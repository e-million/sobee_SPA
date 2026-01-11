namespace sobee_API.DTOs.Orders
{
    public sealed class OrderItemResponse
    {
        public int? OrderItemId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
