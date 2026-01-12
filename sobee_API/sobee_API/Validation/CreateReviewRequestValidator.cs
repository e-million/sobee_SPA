using FluentValidation;
using sobee_API.DTOs.Reviews;

namespace sobee_API.Validation
{
    public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
    {
        public CreateReviewRequestValidator()
        {
            RuleFor(x => x.ReviewText)
                .NotEmpty()
                .WithMessage("ReviewText is required.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");
        }
    }
}
