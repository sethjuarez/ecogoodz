using Microsoft.AspNetCore.Mvc.Filters;

namespace EcoGoodz.Web.Identity;

/// <summary>
/// Redirects any signed-in user still flagged "must change password" to the forced
/// reset page, regardless of which controller/action they hit. Registered globally
/// in Program.cs.
///
/// Deliberately an explicit allow-list of actions (rather than exempting the whole
/// Account controller) so that any future [Authorize] action added to
/// AccountController - e.g. a profile page - doesn't silently bypass the forced
/// reset gate just by living in the same controller.
/// </summary>
public class ForcePasswordChangeFilter : IAsyncActionFilter
{
    private static readonly (string Controller, string Action)[] AllowedWhileMustChange =
    [
        ("Account", "ForcePasswordChange"),
        ("Account", "Logout"),
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();

        var mustChange = user.Identity?.IsAuthenticated == true
            && user.HasClaim(ApplicationClaimsPrincipalFactory.MustChangePasswordClaimType, "true");

        var isAllowed = AllowedWhileMustChange.Any(a =>
            string.Equals(a.Controller, controllerName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Action, actionName, StringComparison.OrdinalIgnoreCase));

        if (mustChange && !isAllowed)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult(
                "ForcePasswordChange", "Account", null);
            return;
        }

        await next();
    }
}

