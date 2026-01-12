using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Reviews
{
    public sealed class ProductReviewsResponseDto
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("reviews")]
        public List<ReviewListItemDto> Reviews { get; set; } = new();
    }
}
