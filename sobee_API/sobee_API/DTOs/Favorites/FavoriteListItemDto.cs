using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Favorites
{
    public sealed class FavoriteListItemDto
    {
        [JsonPropertyName("favoriteId")]
        public int FavoriteId { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("added")]
        public DateTime Added { get; set; }
    }
}
