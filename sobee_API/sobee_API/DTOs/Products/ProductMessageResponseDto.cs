using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductMessageResponseDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
