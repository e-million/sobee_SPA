using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Home
{
    public sealed class HomePingResponseDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("db")]
        public bool Db { get; set; }
    }
}
