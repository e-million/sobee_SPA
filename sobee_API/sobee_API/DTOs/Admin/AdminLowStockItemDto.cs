using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Admin
{
    public sealed class AdminLowStockItemDto
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("stockAmount")]
        public int? StockAmount { get; set; }
    }
}
