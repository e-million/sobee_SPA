namespace sobee_API.DTOs.Products
{
    public sealed class ProductDetailDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool InStock { get; set; }
        public int? StockAmount { get; set; }
        public decimal? Cost { get; set; }
        public string? Category { get; set; }
        public int? CategoryId { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
    }

    public sealed class ProductImageDto
    {
        public int Id { get; set; }
        public string? Url { get; set; }
    }
}
