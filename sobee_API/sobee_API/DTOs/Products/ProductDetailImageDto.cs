using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductDetailImageDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
