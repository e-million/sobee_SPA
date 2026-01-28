using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Auth;
using sobee_API.DTOs.Common;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user with profile details (no token issuance).
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterWithProfileRequest req)
        {
            var email = req.Email.Trim();

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return Conflict(new ApiErrorResponse("A user with this email already exists.", "Conflict"));

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,

                strFirstName = req.FirstName.Trim(),
                strLastName = req.LastName.Trim(),
                strBillingAddress = req.BillingAddress.Trim(),
                strShippingAddress = req.ShippingAddress.Trim(),

                CreatedDate = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, req.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new ApiErrorResponse(
                    "Registration failed.",
                    "ValidationError",
                    new
                    {
                        errors = result.Errors.Select(e => new { e.Code, e.Description }).ToArray()
                    }));
            }

            // Do NOT issue tokens here. Use Identity /identity/login for bearer token issuance.
            return Created("", new
            {
                message = "User created. Call /identity/login to get a bearer token.",
                email = user.Email
            });
        }

        /// <summary>
        /// Request a password reset email.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Ok(new { success = true });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = BuildResetUrl(email, token);

            _logger.LogInformation(
                "Password reset requested for {Email}. Reset URL: {ResetUrl}",
                email,
                resetUrl);

            if (_environment.IsDevelopment())
            {
                return Ok(new
                {
                    success = true,
                    resetUrl
                });
            }

            return Ok(new { success = true });
        }

        /// <summary>
        /// Reset password using the emailed token.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return BadRequest(new ApiErrorResponse(
                    "Reset token is invalid.",
                    "INVALID_TOKEN"));
            }

            var decodedToken = WebUtility.UrlDecode(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
            {
                var errorCodes = result.Errors.Select(e => e.Code).ToList();
                var isTokenInvalid = errorCodes.Any(code => code.Equals("InvalidToken", StringComparison.OrdinalIgnoreCase));

                if (isTokenInvalid)
                {
                    return BadRequest(new ApiErrorResponse(
                        "Reset token is invalid or expired.",
                        "INVALID_TOKEN"));
                }

                return BadRequest(new ApiErrorResponse(
                    "Password reset failed.",
                    "VALIDATION_ERROR",
                    new
                    {
                        errors = result.Errors.Select(e => new { e.Code, e.Description }).ToArray()
                    }));
            }

            return Ok(new { success = true });
        }

        private string BuildResetUrl(string email, string token)
        {
            var baseUrl = _configuration["PasswordReset:FrontendBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = Request.Headers.Origin.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:4200";
            }

            baseUrl = baseUrl.TrimEnd('/');
            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(email);
            return $"{baseUrl}/reset-password?token={encodedToken}&email={encodedEmail}";
        }
    }
}
