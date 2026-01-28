namespace sobee_API.DTOs.Products
{
    public class ProductListDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? Cost { get; set; }

        // Admin-only in responses (we'll conditionally populate)
        public int? StockAmount { get; set; }

        // Public-facing
        public bool InStock { get; set; }
        public string? PrimaryImageUrl { get; set; }

        public string? Category { get; set; }
        public int? CategoryId { get; set; }

        // Admin-only in responses
        public bool? IsActive { get; set; }
    }
}
