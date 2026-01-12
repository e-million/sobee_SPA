using FluentValidation;
using sobee_API.DTOs.Products;

namespace sobee_API.Validation
{
    public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => x.Name != null)
                .WithMessage("Name cannot be empty.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Price.HasValue)
                .WithMessage("Price cannot be negative.");

            RuleFor(x => x.StockAmount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.StockAmount.HasValue)
                .WithMessage("StockAmount cannot be negative.");
        }
    }
}
