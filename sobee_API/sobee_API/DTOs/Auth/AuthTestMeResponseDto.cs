using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Auth
{
    public sealed class AuthTestMeResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("roles")]
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
