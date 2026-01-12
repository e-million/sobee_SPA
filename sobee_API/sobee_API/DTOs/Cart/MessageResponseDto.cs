using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Cart
{
    public sealed class MessageResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
