using GymClub.Domain.Entities;
using GymClub.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymClub.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");
        var userManager = sp.GetRequiredService<UserManager<User>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // نقش‌ها
        foreach (var role in new[] { "User", "ContentWriter", "ShopManager", "Trainer", "Admin", "SuperAdmin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // سوپر ادمین
        const string superAdminPhone = "09120000000";
        const string superAdminPassword = "SuperAdmin@123";

        var superAdmin = await userManager.FindByNameAsync(superAdminPhone);
        if (superAdmin is null)
        {
            superAdmin = new User
            {
                UserName = superAdminPhone,
                PhoneNumber = superAdminPhone,
                PhoneNumberConfirmed = true,
                IsPhoneConfirmed = true,
                FullName = "Super Admin",
                DisplayUserName = "superadmin",
                Role = UserRole.SuperAdmin,
                IsActive = true
            };

            var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
        else
        {
            // اگر از قبل بود و نقش ندارد
            if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");

            if (!superAdmin.IsActive)
            {
                superAdmin.IsActive = true;
                await userManager.UpdateAsync(superAdmin);
            }
        }
    }
}