using FluentValidation;
using sobee_API.DTOs.Products;

namespace sobee_API.Validation
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price cannot be negative.");

            RuleFor(x => x.StockAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("StockAmount cannot be negative.");
        }
    }
}
