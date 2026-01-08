using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sobee.Domain.Identity;

namespace sobee_API.Services;

public static class IdentitySeedService
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        // Gate seeding so it doesn't run unexpectedly in prod
        var enabled = config["SeedAdmin:Enabled"];
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Identity seeding skipped (SeedAdmin:Enabled != true).");
            return;
        }

        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1) Ensure roles exist
        await EnsureRoleAsync(roleManager, "Admin", logger);
        await EnsureRoleAsync(roleManager, "User", logger);

        // 2) Ensure admin user exists
        var email = config["SeedAdmin:Email"];
        var password = config["SeedAdmin:Password"];
        var firstName = config["SeedAdmin:FirstName"] ?? "Admin";
        var lastName = config["SeedAdmin:LastName"] ?? "User";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SeedAdmin enabled but Email/Password not provided. Skipping admin user creation.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            // IMPORTANT: your ApplicationUser currently has NOT NULL fields in DB.
            // Populate them here so insert doesn't fail.
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,

                strFirstName = firstName,
                strLastName = lastName,

                // Set non-null defaults; you can refine later
                strBillingAddress = config["SeedAdmin:BillingAddress"] ?? "N/A",
                strShippingAddress = config["SeedAdmin:ShippingAddress"] ?? "N/A",

                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(admin, password);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Failed to create seeded admin user: {msg}");
            }

            existing = admin;
            logger.LogInformation("Seeded admin user created: {Email}", email);
        }
        else
        {
            logger.LogInformation("Seeded admin user already exists: {Email}", email);
        }

        // 3) Ensure admin role assignment
        if (!await userManager.IsInRoleAsync(existing, "Admin"))
        {
            var addRole = await userManager.AddToRoleAsync(existing, "Admin");
            if (!addRole.Succeeded)
            {
                var msg = string.Join("; ", addRole.Errors.Select(e => $"{e.Code}:{e.Description}"));
                throw new InvalidOperationException($"Failed to assign Admin role: {msg}");
            }

            logger.LogInformation("Admin role assigned to: {Email}", email);
        }
        else
        {
            logger.LogInformation("User already in Admin role: {Email}", email);
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName, ILogger logger)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var res = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!res.Succeeded)
        {
            var msg = string.Join("; ", res.Errors.Select(e => $"{e.Code}:{e.Description}"));
            throw new InvalidOperationException($"Failed to create role '{roleName}': {msg}");
        }

        logger.LogInformation("Role created: {Role}", roleName);
    }
}
