using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Auth
{
    public sealed class AuthTestAdminOnlyResponseDto
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
