namespace sobee_API.DTOs.Products
{
    public sealed class ProductListResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<ProductListDto> Items { get; set; } = new();
    }
}
