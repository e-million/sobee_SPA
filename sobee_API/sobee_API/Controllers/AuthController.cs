using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Identity;

namespace sobee_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public sealed class RegisterWithProfileRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;

            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;

            public string BillingAddress { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterWithProfileRequest req)
        {
            // Hard requirements (since you want these not-null and you’ll use them later)
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { error = "Email is required." });

            if (string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Password is required." });

            if (string.IsNullOrWhiteSpace(req.FirstName))
                return BadRequest(new { error = "FirstName is required." });

            if (string.IsNullOrWhiteSpace(req.LastName))
                return BadRequest(new { error = "LastName is required." });

            if (string.IsNullOrWhiteSpace(req.BillingAddress))
                return BadRequest(new { error = "BillingAddress is required." });

            if (string.IsNullOrWhiteSpace(req.ShippingAddress))
                return BadRequest(new { error = "ShippingAddress is required." });

            var email = req.Email.Trim();

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return Conflict(new { error = "A user with this email already exists." });

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
                return BadRequest(new
                {
                    error = "Registration failed.",
                    details = result.Errors.Select(e => new { e.Code, e.Description })
                });
            }

            // Do NOT issue tokens here. Use Identity /identity/login for bearer token issuance.
            return Created("", new
            {
                message = "User created. Call /identity/login to get a bearer token.",
                email = user.Email
            });
        }
    }
}
