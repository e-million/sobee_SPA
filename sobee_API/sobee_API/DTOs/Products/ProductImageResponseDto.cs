namespace sobee_API.DTOs.Products
{
    public sealed class ProductImageResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? Url { get; set; }
    }
}
