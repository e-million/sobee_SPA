using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Favorites
{
    public sealed class FavoriteStatusResponseDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }
    }
}
