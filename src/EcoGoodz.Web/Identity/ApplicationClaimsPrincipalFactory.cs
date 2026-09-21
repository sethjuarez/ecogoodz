using System.Security.Claims;
using EcoGoodz.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace EcoGoodz.Web.Identity;

/// <summary>
/// Adds a "must change password" claim to the sign-in cookie so the global
/// <see cref="ForcePasswordChangeFilter"/> can check it cheaply without a DB call
/// on every request. Call SignInManager.RefreshSignInAsync(user) after the flag
/// changes to re-issue the cookie with an up-to-date claim.
/// </summary>
public class ApplicationClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<int>>
{
    public const string MustChangePasswordClaimType = "must_change_password";
    public const string LegacyUserIdClaimType = "legacy_user_id";

    public ApplicationClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(MustChangePasswordClaimType, user.MustChangePassword ? "true" : "false"));
        identity.AddClaim(new Claim(LegacyUserIdClaimType, user.LegacyUserId.ToString()));
        return identity;
    }
}
