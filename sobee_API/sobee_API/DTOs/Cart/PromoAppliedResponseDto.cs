using System.Text.Json.Serialization;

namespace sobee_API.DTOs.Cart
{
    public sealed class PromoAppliedResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("promoCode")]
        public string PromoCode { get; init; } = string.Empty;

        [JsonPropertyName("discountPercentage")]
        public decimal DiscountPercentage { get; init; }
    }
}
