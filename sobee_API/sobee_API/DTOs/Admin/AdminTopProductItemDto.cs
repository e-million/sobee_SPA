using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Admin
{
    public sealed class AdminTopProductItemDto
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("quantitySold")]
        public int QuantitySold { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }
    }
}
