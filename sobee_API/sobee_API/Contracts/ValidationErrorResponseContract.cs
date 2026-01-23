using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sobee_API.Contracts;

public sealed class ValidationErrorResponseContract
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }

    [JsonPropertyName("details")]
    public ValidationErrorDetailsContract Details { get; init; } = new();
}

public sealed class ValidationErrorDetailsContract
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; init; } = new();
}
