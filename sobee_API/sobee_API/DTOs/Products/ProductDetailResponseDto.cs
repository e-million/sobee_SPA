using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductDetailResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("inStock")]
        public bool InStock { get; set; }

        [JsonPropertyName("stockAmount")]
        public int? StockAmount { get; set; }

        [JsonPropertyName("images")]
        public List<ProductDetailImageDto> Images { get; set; } = new();
    }
}
