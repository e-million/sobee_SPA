namespace sobee_API.DTOs.Orders
{
    public sealed class CheckoutRequest
    {
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public int? PaymentMethodId { get; set; }
    }
}
