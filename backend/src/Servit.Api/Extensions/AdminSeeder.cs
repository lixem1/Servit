using Microsoft.AspNetCore.Identity;
using Servit.Domain.Constants;
using Servit.Domain.Entities;

namespace Servit.Api.Extensions;

// Seeds the initial administrator from configuration (Admin:Email / Admin:Password),
// creating the Admin role if missing. Idempotent: safe to run on every startup.
// If the credentials aren't configured it logs a warning and does nothing, so the
// API still boots in environments where the admin hasn't been provisioned yet.
public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Admin seed skipped: Admin:Email and/or Admin:Password are not configured. " +
                "Set them via user-secrets or environment to provision the initial administrator.");
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin));
            logger.LogInformation("Created role {Role}.", Roles.Admin);
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Administrador",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create admin user {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Created admin user {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
            logger.LogInformation("Assigned role {Role} to {Email}.", Roles.Admin, email);
        }
    }
}
