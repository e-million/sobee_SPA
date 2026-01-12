using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sobee.Domain.Identity;
using sobee_API.DTOs.Auth;

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

        /// <summary>
        /// Register a new user with profile details (no token issuance).
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterWithProfileRequest req)
        {
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
