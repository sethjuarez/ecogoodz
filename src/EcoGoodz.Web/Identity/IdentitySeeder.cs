using Microsoft.AspNetCore.Identity;

namespace EcoGoodz.Web.Identity;

/// <summary>
/// Ensures the fixed set of Identity roles exists. Run once at startup - idempotent.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }
    }
}
