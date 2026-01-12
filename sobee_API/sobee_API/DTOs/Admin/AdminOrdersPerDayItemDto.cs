using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Admin
{
    public sealed class AdminOrdersPerDayItemDto
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }
    }
}
