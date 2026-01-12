using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Reviews
{
    public sealed class ReviewReplyDto
    {
        [JsonPropertyName("replyId")]
        public int ReplyId { get; set; }

        [JsonPropertyName("reviewId")]
        public int ReviewId { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }
    }
}
