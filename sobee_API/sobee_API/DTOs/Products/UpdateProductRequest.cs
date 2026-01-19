namespace sobee_API.DTOs.Products
{
    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? Cost { get; set; }
        public int? StockAmount { get; set; }
        public int? CategoryId { get; set; }
    }
}
