using FluentValidation;
using sobee_API.Constants;
using sobee_API.DTOs.Orders;
using System;
using System.Linq;

namespace sobee_API.Validation
{
    public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
    {
        public UpdateOrderStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.")
                .Must(status => OrderStatuses.All.Any(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase)))
                .WithMessage($"Status must be one of: {string.Join(", ", OrderStatuses.All)}");
        }
    }
}
