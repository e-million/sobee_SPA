using FluentValidation;
using sobee_API.DTOs.Products;
using System;

namespace sobee_API.Validation
{
    public class AddProductImageRequestValidator : AbstractValidator<AddProductImageRequest>
    {
        public AddProductImageRequestValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty()
                .WithMessage("Url is required.")
                .Must(BeAbsoluteHttpUrl)
                .WithMessage("Url must be an absolute http or https URL.");
        }

        private static bool BeAbsoluteHttpUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                && (string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }
    }
}
