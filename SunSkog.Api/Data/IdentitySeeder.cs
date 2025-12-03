using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SunSkog.Api.Models;

namespace SunSkog.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        const string adminRole = "Admin";
        const string userRole  = "User";
        var email = "admin@sunskog.local";
        var pwd   = "Admin123!";

        // Role
        if (!await roles.RoleExistsAsync(adminRole))
            await roles.CreateAsync(new IdentityRole(adminRole));

        if (!await roles.RoleExistsAsync(userRole))
            await roles.CreateAsync(new IdentityRole(userRole));

        // Admin user
        var admin = await users.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Administrator"
            };

            var createRes = await users.CreateAsync(admin, pwd);
            if (!createRes.Succeeded)
            {
                var msg = string.Join("; ", createRes.Errors.Select(e => $"{e.Code}:{e.Description}"));
                logger.LogError("Admin create failed: {Errors}", msg);
                return;
            }
        }

        // Přiřazení role
        var rolesForAdmin = await users.GetRolesAsync(admin);
        if (!rolesForAdmin.Contains(adminRole))
            await users.AddToRoleAsync(admin, adminRole);

        logger.LogInformation("Identity seed finished. Admin: {Email}", email);
    }
}
