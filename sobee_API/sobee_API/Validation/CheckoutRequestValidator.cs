using FluentValidation;
using sobee_API.DTOs.Orders;

namespace sobee_API.Validation
{
    public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
    {
        public CheckoutRequestValidator()
        {
            RuleFor(x => x.ShippingAddress)
                .NotEmpty()
                .WithMessage("ShippingAddress is required.");

            RuleFor(x => x.PaymentMethodId)
                .NotNull()
                .WithMessage("PaymentMethodId must be a positive integer.")
                .GreaterThan(0)
                .WithMessage("PaymentMethodId must be a positive integer.");
        }
    }
}
