using Microsoft.AspNetCore.Identity;
using Sobee.Domain.Identity;
using System.Collections.Generic;
using System.Linq;

namespace sobee_API.Services
{
    public class AdminSeedService
    {
        private const string AdminRoleName = "Admin";
        private const string CustomerRoleName = "Customer";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSeedService> _logger;

        public AdminSeedService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AdminSeedService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var email = _configuration["Admin:Email"];
            var password = _configuration["Admin:Password"];
            var firstName = _configuration["Admin:FirstName"];
            var lastName = _configuration["Admin:LastName"];
            var billingAddress = _configuration["Admin:BillingAddress"];
            var shippingAddress = _configuration["Admin:ShippingAddress"];

            var missingKeys = new List<string>();
            if (string.IsNullOrWhiteSpace(email))
            {
                missingKeys.Add("Admin:Email");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                missingKeys.Add("Admin:Password");
            }

            if (missingKeys.Count > 0)
            {
                foreach (var key in missingKeys)
                {
                    _logger.LogWarning("Admin seeding skipped: {MissingKey} missing.", key);
                }
                return;
            }

            try
            {
                await EnsureRoleAsync(AdminRoleName);
                await EnsureRoleAsync(CustomerRoleName);

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        strFirstName = string.IsNullOrWhiteSpace(firstName) ? "Admin" : firstName,
                        strLastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName,
                        strBillingAddress = string.IsNullOrWhiteSpace(billingAddress) ? "N/A" : billingAddress,
                        strShippingAddress = string.IsNullOrWhiteSpace(shippingAddress) ? "N/A" : shippingAddress,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user, password);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                        _logger.LogError("Failed to create admin user: {Errors}", errors);
                        return;
                    }

                    _logger.LogInformation("Created admin user {Email}.", email);
                }
                else
                {
                    var shouldUpdate = false;
                    if (!string.IsNullOrWhiteSpace(firstName) && user.strFirstName != firstName)
                    {
                        user.strFirstName = firstName;
                        shouldUpdate = true;
                    }

                    if (!string.IsNullOrWhiteSpace(lastName) && user.strLastName != lastName)
                    {
                        user.strLastName = lastName;
                        shouldUpdate = true;
                    }

                    if (!string.IsNullOrWhiteSpace(billingAddress) && user.strBillingAddress != billingAddress)
                    {
                        user.strBillingAddress = billingAddress;
                        shouldUpdate = true;
                    }

                    if (!string.IsNullOrWhiteSpace(shippingAddress) && user.strShippingAddress != shippingAddress)
                    {
                        user.strShippingAddress = shippingAddress;
                        shouldUpdate = true;
                    }

                    if (shouldUpdate)
                    {
                        var updateResult = await _userManager.UpdateAsync(user);
                        if (!updateResult.Succeeded)
                        {
                            var errors = string.Join(", ", updateResult.Errors.Select(error => error.Description));
                            _logger.LogError("Failed to update admin user profile: {Errors}", errors);
                        }
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, AdminRoleName))
                {
                    var roleResult = await _userManager.AddToRoleAsync(user, AdminRoleName);
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                        _logger.LogError("Failed to assign admin role: {Errors}", errors);
                    }
                }

                _logger.LogInformation("Admin seeding completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin seeding failed.");
            }
        }

        private async Task EnsureRoleAsync(string roleName)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
            }
        }
    }
}
