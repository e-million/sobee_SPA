using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Products
{
    public sealed class ProductAdminResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("stockAmount")]
        public int? StockAmount { get; set; }
    }
}
