using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Common;
using sobee_API.DTOs.Users;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return UnauthorizedError("User not found.", "Unauthorized");

            return Ok(ToProfileResponse(user));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return UnauthorizedError("User not found.", "Unauthorized");

            var validationErrors = ValidateProfileRequest(request);
            if (validationErrors.Count > 0)
                return ValidationError("Validation failed.", validationErrors);

            var trimmedEmail = request.Email.Trim();

            if (!string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(trimmedEmail);
                if (existing != null && existing.Id != user.Id)
                    return ConflictError("A user with this email already exists.", "EMAIL_EXISTS", new { field = "email" });

                var setEmailResult = await _userManager.SetEmailAsync(user, trimmedEmail);
                if (!setEmailResult.Succeeded)
                    return IdentityValidationError(setEmailResult, ResolveProfileField);

                var setUserNameResult = await _userManager.SetUserNameAsync(user, trimmedEmail);
                if (!setUserNameResult.Succeeded)
                    return IdentityValidationError(setUserNameResult, ResolveProfileField);
            }

            user.strFirstName = request.FirstName.Trim();
            user.strLastName = request.LastName.Trim();
            user.strBillingAddress = request.BillingAddress.Trim();
            user.strShippingAddress = request.ShippingAddress.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return IdentityValidationError(updateResult, ResolveProfileField);

            return Ok(ToProfileResponse(user));
        }

        [HttpPut("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return UnauthorizedError("User not found.", "Unauthorized");

            var validationErrors = ValidatePasswordRequest(request);
            if (validationErrors.Count > 0)
                return ValidationError("Validation failed.", validationErrors);

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return IdentityValidationError(result, ResolvePasswordField);

            return Ok(new { success = true });
        }

        private static UserProfileResponse ToProfileResponse(ApplicationUser user)
        {
            return new UserProfileResponse
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.strFirstName,
                LastName = user.strLastName,
                BillingAddress = user.strBillingAddress,
                ShippingAddress = user.strShippingAddress
            };
        }

        private static List<object> ValidateProfileRequest(UpdateUserProfileRequest request)
        {
            var errors = new List<object>();

            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add(new { field = "email", message = "Email is required." });
            if (string.IsNullOrWhiteSpace(request.FirstName))
                errors.Add(new { field = "firstName", message = "First name is required." });
            if (string.IsNullOrWhiteSpace(request.LastName))
                errors.Add(new { field = "lastName", message = "Last name is required." });
            if (string.IsNullOrWhiteSpace(request.BillingAddress))
                errors.Add(new { field = "billingAddress", message = "Billing address is required." });
            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                errors.Add(new { field = "shippingAddress", message = "Shipping address is required." });

            return errors;
        }

        private static List<object> ValidatePasswordRequest(UpdatePasswordRequest request)
        {
            var errors = new List<object>();

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                errors.Add(new { field = "currentPassword", message = "Current password is required." });
            if (string.IsNullOrWhiteSpace(request.NewPassword))
                errors.Add(new { field = "newPassword", message = "New password is required." });

            return errors;
        }

        private ObjectResult ValidationError(string message, IEnumerable<object> errors)
            => StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiErrorResponse(message, "ValidationError", new { errors }));

        private ObjectResult IdentityValidationError(IdentityResult result, Func<string, string> fieldResolver)
        {
            var errors = result.Errors.Select(error => new
            {
                field = fieldResolver(error.Code),
                message = error.Description,
                code = error.Code
            });

            return ValidationError("Validation failed.", errors);
        }

        private static string ResolveProfileField(string code)
        {
            if (code.Contains("Email", StringComparison.OrdinalIgnoreCase))
                return "email";

            return "profile";
        }

        private static string ResolvePasswordField(string code)
        {
            if (code.Contains("PasswordMismatch", StringComparison.OrdinalIgnoreCase))
                return "currentPassword";

            return "newPassword";
        }

    }
}
