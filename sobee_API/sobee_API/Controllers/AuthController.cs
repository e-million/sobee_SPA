using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Sobee.Domain.Identity;

namespace sobee_API.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager, ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var missingFields = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                missingFields.Add("email");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                missingFields.Add("password");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                missingFields.Add("firstName");
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                missingFields.Add("lastName");
            }

            if (missingFields.Count > 0)
            {
                return BadRequest(new
                {
                    error = "Missing required fields.",
                    fields = missingFields
                });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "A user with this email already exists." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                strFirstName = request.FirstName,
                strLastName = request.LastName,
                strBillingAddress = string.IsNullOrWhiteSpace(request.BillingAddress) ? "N/A" : request.BillingAddress,
                strShippingAddress = string.IsNullOrWhiteSpace(request.ShippingAddress) ? "N/A" : request.ShippingAddress,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(error => error.Description).ToArray();
                _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, string.Join(", ", errors));
                return BadRequest(new { error = "Registration failed.", details = errors });
            }

            return Ok(new { message = "Registration successful." });
        }
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? BillingAddress,
        string? ShippingAddress);
}
