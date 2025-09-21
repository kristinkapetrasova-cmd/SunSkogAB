using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using SunSkog.Api.Data;
using SunSkog.Api.Models;

namespace SunSkog.Api.Data
{
    public static class Seed
    {
        /// <summary>
        /// Aplikuje migrace a nasází základní role + admin uživatele (dev účely).
        /// </summary>
        public static async Task EnsureSeeded(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Role
            string[] roles = new[] { "SuperAdmin", "Admin", "Employee" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var r = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!r.Succeeded)
                        throw new InvalidOperationException("Failed to create role '" + role + "': " + string.Join(", ", r.Errors.Select(e => e.Description)));
                }
            }

            // Admin user (dev)
            const string adminEmail = "admin@sunskog.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Super Admin",
                    EmailConfirmed = true
                };

                var create = await userManager.CreateAsync(admin, "Admin123$");
                if (!create.Succeeded)
                    throw new InvalidOperationException("Failed to create admin user: " + string.Join(", ", create.Errors.Select(e => e.Description)));

                var addRole = await userManager.AddToRoleAsync(admin, "SuperAdmin");
                if (!addRole.Succeeded)
                    throw new InvalidOperationException("Failed to assign role to admin: " + string.Join(", ", addRole.Errors.Select(e => e.Description)));
            }
        }
    }
}