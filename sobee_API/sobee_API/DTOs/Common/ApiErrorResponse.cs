using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Common
{
    public sealed class ApiErrorResponse
    {
        public ApiErrorResponse() { }

        public ApiErrorResponse(string error, string? code = null, object? details = null)
        {
            Error = error;
            Code = code;
            Details = details;
        }

        [JsonPropertyName("error")]
        public string Error { get; init; } = string.Empty;

        [JsonPropertyName("code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }

        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Details { get; init; }
    }
}
