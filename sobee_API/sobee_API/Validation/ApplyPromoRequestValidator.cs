using FluentValidation;
using sobee_API.DTOs;

namespace sobee_API.Validation
{
    public class ApplyPromoRequestValidator : AbstractValidator<ApplyPromoRequest>
    {
        public ApplyPromoRequestValidator()
        {
            RuleFor(x => x.PromoCode)
                .NotEmpty()
                .WithMessage("PromoCode is required.")
                .MaximumLength(50)
                .WithMessage("PromoCode must be 50 characters or less.");
        }
    }
}
