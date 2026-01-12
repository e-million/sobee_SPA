using FluentValidation;
using sobee_API.DTOs.Orders;

namespace sobee_API.Validation
{
    public class PayOrderRequestValidator : AbstractValidator<PayOrderRequest>
    {
        public PayOrderRequestValidator()
        {
            RuleFor(x => x.PaymentMethodId)
                .GreaterThan(0)
                .WithMessage("PaymentMethodId must be a positive integer.");
        }
    }
}
