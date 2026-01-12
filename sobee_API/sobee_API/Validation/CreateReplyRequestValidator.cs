using FluentValidation;
using sobee_API.DTOs.Reviews;

namespace sobee_API.Validation
{
    public class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
    {
        public CreateReplyRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required.");
        }
    }
}
