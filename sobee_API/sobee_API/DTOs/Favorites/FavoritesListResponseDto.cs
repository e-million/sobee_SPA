using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Favorites
{
    public sealed class FavoritesListResponseDto
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("favorites")]
        public List<FavoriteListItemDto> Favorites { get; set; } = new();
    }
}
