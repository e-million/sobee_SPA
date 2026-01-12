using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Common
{
    public sealed class ApiErrorResponse
    {
        public ApiErrorResponse() { }

        public ApiErrorResponse(string error, string? code = null)
        {
            Error = error;
            Code = code;
        }

        [JsonPropertyName("error")]
        public string Error { get; init; } = string.Empty;

        [JsonPropertyName("code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }
    }
}
