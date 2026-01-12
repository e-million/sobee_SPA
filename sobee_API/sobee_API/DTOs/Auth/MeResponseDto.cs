using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Auth
{
    public sealed class MeResponseDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("roles")]
        public string[] Roles { get; set; } = Array.Empty<string>();

        [JsonPropertyName("claims")]
        public MeClaimDto[] Claims { get; set; } = Array.Empty<MeClaimDto>();
    }
}
