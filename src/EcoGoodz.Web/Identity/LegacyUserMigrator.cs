using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Identity;

/// <summary>
/// One-time bootstrap: creates an ApplicationUser (no password set) for every legacy
/// User row that doesn't have one yet, in the matching Identity role. Users complete
/// setup via the "Forgot your password?" flow, which sets their password for the
/// first time. Idempotent - safe to run on every startup. Gated by the
/// "Migration:SeedLegacyUsers" configuration flag so it never runs unintentionally
/// against a real production database.
/// </summary>
public static class LegacyUserMigrator
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        if (!config.GetValue<bool>("Migration:SeedLegacyUsers"))
        {
            return;
        }

        var dataContext = services.GetRequiredService<EcoGoodzDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var alreadyMigratedLegacyIds = await userManager.Users
            .Select(u => u.LegacyUserId)
            .ToListAsync();

        var legacyUsers = await dataContext.Users
            .Include(u => u.RoleNavigation)
            .Where(u => !alreadyMigratedLegacyIds.Contains(u.Id))
            .ToListAsync();

        foreach (var legacyUser in legacyUsers)
        {
            var userName = !string.IsNullOrWhiteSpace(legacyUser.UserName)
                ? legacyUser.UserName
                : legacyUser.Email ?? $"user{legacyUser.Id}@ecogoodz.com";

            var applicationUser = new ApplicationUser
            {
                UserName = userName,
                Email = legacyUser.Email,
                LegacyUserId = legacyUser.Id,
                MustChangePassword = true,
                LockoutEnabled = true,
            };

            var createResult = await userManager.CreateAsync(applicationUser);
            if (!createResult.Succeeded)
            {
                continue;
            }

            var roleName = legacyUser.RoleNavigation?.RoleName;
            if (!string.IsNullOrEmpty(roleName) && AppRoles.All.Contains(roleName))
            {
                await userManager.AddToRoleAsync(applicationUser, roleName);
            }
        }
    }
}
