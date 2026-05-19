using Microsoft.AspNetCore.Identity;
using EFNco.Models;

namespace EFNco.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed Roles — Sprint 4 adds Guard
            string[] roles = { "Admin", "Guard" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed Default Admin
            const string adminEmail = "admin@efnco.com";
            const string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    Department = "IT",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Default Guard
            const string guardEmail = "guard@efnco.com";
            const string guardPassword = "Guard@123";

            if (await userManager.FindByEmailAsync(guardEmail) == null)
            {
                var guard = new ApplicationUser
                {
                    UserName = guardEmail,
                    Email = guardEmail,
                    FirstName = "Gate",
                    LastName = "Guard",
                    Department = "Security",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(guard, guardPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(guard, "Guard");
            }
        }
    }
}
