using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Reviews
{
    public sealed class ReviewDeletedResponseDto
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("reviewId")]
        public int ReviewId { get; set; }
    }
}
