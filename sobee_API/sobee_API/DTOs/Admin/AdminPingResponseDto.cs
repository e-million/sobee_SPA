using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Admin
{
    public sealed class AdminPingResponseDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("area")]
        public string? Area { get; set; }
    }
}
