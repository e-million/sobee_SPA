using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Admin
{
    public sealed class AdminSummaryResponseDto
    {
        [JsonPropertyName("totalOrders")]
        public int TotalOrders { get; set; }

        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [JsonPropertyName("totalDiscounts")]
        public decimal TotalDiscounts { get; set; }

        [JsonPropertyName("averageOrderValue")]
        public decimal AverageOrderValue { get; set; }
    }
}
