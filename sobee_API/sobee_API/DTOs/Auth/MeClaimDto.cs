using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Auth
{
    public sealed class MeClaimDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
