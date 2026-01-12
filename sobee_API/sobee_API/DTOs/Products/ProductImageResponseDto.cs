using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductImageResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
