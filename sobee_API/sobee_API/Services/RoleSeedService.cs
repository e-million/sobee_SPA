using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace sobee_API.Services
{
    public sealed class RoleSeedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoleSeedService> _logger;

        public RoleSeedService(IServiceProvider serviceProvider, ILogger<RoleSeedService> logger) {

            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken) {

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await EnsureRoleAsync(roleManager, "Admin");
                await EnsureRoleAsync(roleManager, "User");

                _logger.LogInformation("Role seeding completed.");

            }

            catch (Exception ex) {
                // Do not crash the APU just because seeding failed
                _logger.LogError(ex, "Role seeding failed"); 
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (await roleManager.RoleExistsAsync(roleName)) return;

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var msg = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException($"Failed creating role '{roleName}': {msg}");
            }
        }
    }
}