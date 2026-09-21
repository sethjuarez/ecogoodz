using Microsoft.AspNetCore.Mvc.Filters;

namespace EcoGoodz.Web.Identity;

/// <summary>
/// Redirects any signed-in user still flagged "must change password" to the forced
/// reset page, regardless of which controller/action they hit. Registered globally
/// in Program.cs. Allows the Account controller itself and static assets through.
/// </summary>
public class ForcePasswordChangeFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controllerName = context.RouteData.Values["controller"]?.ToString();

        var mustChange = user.Identity?.IsAuthenticated == true
            && user.HasClaim(ApplicationClaimsPrincipalFactory.MustChangePasswordClaimType, "true");

        if (mustChange && !string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult(
                "ForcePasswordChange", "Account", null);
            return;
        }

        await next();
    }
}
