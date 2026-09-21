using System.Security.Claims;

namespace EcoGoodz.Web.Identity;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's Id in the legacy [User] business table (as opposed to
    /// their ASP.NET Core Identity Id), for stamping CreatedBy/UpdatedBy columns.
    /// </summary>
    public static int? GetLegacyUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ApplicationClaimsPrincipalFactory.LegacyUserIdClaimType);
        return int.TryParse(value, out var id) ? id : null;
    }
}
