using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Reviews
{
    public sealed class ReviewListItemDto
    {
        [JsonPropertyName("reviewId")]
        public int ReviewId { get; set; }

        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("reviewText")]
        public string? ReviewText { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("replies")]
        public List<ReviewReplyDto> Replies { get; set; } = new();
    }
}
