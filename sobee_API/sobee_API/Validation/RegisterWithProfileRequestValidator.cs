using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using sobee_API.DTOs.Auth;

namespace sobee_API.Validation
{
    public class RegisterWithProfileRequestValidator : AbstractValidator<RegisterWithProfileRequest>
    {
        public RegisterWithProfileRequestValidator(IOptions<IdentityOptions> identityOptions)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("FirstName is required.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("LastName is required.");

            RuleFor(x => x.BillingAddress)
                .NotEmpty()
                .WithMessage("BillingAddress is required.");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty()
                .WithMessage("ShippingAddress is required.");

            var passwordOptions = identityOptions.Value.Password ?? new PasswordOptions();
            var defaultOptions = new PasswordOptions();
            var hasConfiguredOptions =
                passwordOptions.RequiredLength != defaultOptions.RequiredLength
                || passwordOptions.RequireDigit != defaultOptions.RequireDigit
                || passwordOptions.RequireLowercase != defaultOptions.RequireLowercase
                || passwordOptions.RequireUppercase != defaultOptions.RequireUppercase
                || passwordOptions.RequireNonAlphanumeric != defaultOptions.RequireNonAlphanumeric;

            if (hasConfiguredOptions)
            {
                if (passwordOptions.RequiredLength > 0)
                {
                    RuleFor(x => x.Password)
                        .MinimumLength(passwordOptions.RequiredLength)
                        .WithMessage($"Password must be at least {passwordOptions.RequiredLength} characters long.");
                }

                if (passwordOptions.RequireUppercase)
                {
                    RuleFor(x => x.Password)
                        .Matches("[A-Z]")
                        .WithMessage("Password must contain an uppercase letter.");
                }

                if (passwordOptions.RequireLowercase)
                {
                    RuleFor(x => x.Password)
                        .Matches("[a-z]")
                        .WithMessage("Password must contain a lowercase letter.");
                }

                if (passwordOptions.RequireDigit)
                {
                    RuleFor(x => x.Password)
                        .Matches("[0-9]")
                        .WithMessage("Password must contain a digit.");
                }

                if (passwordOptions.RequireNonAlphanumeric)
                {
                    RuleFor(x => x.Password)
                        .Matches("[^a-zA-Z0-9]")
                        .WithMessage("Password must contain a non-alphanumeric character.");
                }
            }
        }
    }
}
