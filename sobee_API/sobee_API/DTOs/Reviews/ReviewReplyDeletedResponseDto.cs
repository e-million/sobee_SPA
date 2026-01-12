using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Reviews
{
    public sealed class ReviewReplyDeletedResponseDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("replyId")]
        public int ReplyId { get; set; }
    }
}
