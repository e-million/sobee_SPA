using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductsListResponseDto
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("items")]
        public List<ProductListDto> Items { get; set; } = new();
    }
}
