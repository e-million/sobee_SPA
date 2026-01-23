using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Favorites
{
    public sealed class FavoriteListResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<FavoriteItemDto> Favorites { get; set; } = new();
    }

    public sealed class FavoriteItemDto
    {
        public int FavoriteId { get; set; }
        public int ProductId { get; set; }
        public DateTime Added { get; set; }
    }

    public sealed class FavoriteAddResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ProductId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? FavoriteId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? Added { get; set; }
    }

    public sealed class FavoriteRemoveResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int ProductId { get; set; }
    }
}
