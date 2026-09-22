using EcoGoodz.Data.Identity;
using EcoGoodz.Web.Email;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace EcoGoodz.Web.Controllers;

// Login/ForgotPassword/ResetPassword are unauthenticated by design, so they're the
// only user-facing surface an attacker can hammer directly (username enumeration,
// reset-email flooding). Rate-limited per-IP via the "auth" policy in Program.cs.
[EnableRateLimiting("auth")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _environment = environment;
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(model.UserNameOrEmail);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked out. Try again later.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (user.MustChangePassword)
        {
            return RedirectToAction(nameof(ForcePasswordChange));
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ForcePasswordChange()
    {
        return View(new ForcePasswordChangeViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForcePasswordChange(ForcePasswordChangeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        // User has no usable current password (legacy plaintext password was never
        // migrated), so remove + add rather than ChangePasswordAsync.
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        var addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        // Re-issue the auth cookie so the "must change password" claim clears.
        await _signInManager.RefreshSignInAsync(user);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);

        // Always show the same confirmation, whether or not the account exists,
        // so this can't be used to enumerate valid emails.
        if (user is null)
        {
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetUrl = Url.Action(nameof(ResetPassword), "Account",
            new { email = model.Email, t = encodedToken }, Request.Scheme);

        // For local/dev validation the link can be surfaced directly in the UI
        // in addition to the real email send. Both conditions below
        // must hold - environment name alone is not trusted, since an accidental
        // "Development" value on the live server would otherwise let anyone take
        // over any migrated (passwordless) account instantly.
        if (_environment.IsDevelopment() && _configuration.GetValue("DevTools:ShowResetLinkInResponse", false))
        {
            TempData["DevResetUrl"] = resetUrl;
        }

        // Always attempt the real send too. Failures
        // are logged, not surfaced: the confirmation page must look identical
        // whether or not the account exists AND whether or not the send succeeded,
        // or the response itself becomes an enumeration/oracle signal.
        try
        {
            var body = $"<p>A password reset was requested for your EcoGoodz account.</p>" +
                       $"<p><a href=\"{resetUrl}\">Click here to reset your password</a></p>" +
                       $"<p>If you didn't request this, you can ignore this email.</p>";
            await _emailSender.SendAsync(model.Email, "Reset your EcoGoodz password", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", model.Email);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, [FromQuery(Name = "t")] string encodedToken)
    {
        // NOTE: the query string key ("t") is deliberately different from the
        // ResetPasswordViewModel.Token property name. ASP.NET Core's Input Tag
        // Helper resolves "asp-for" values by first checking ModelState for a
        // case-insensitive match on the property name; if the bound query key
        // were "token" it would collide with "Token" and the hidden field would
        // render the still-encoded raw query value instead of the decoded one.
        SetResetPasswordSecurityHeaders();

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            // Malformed/tampered token - don't leak details, just treat it like an
            // expired/invalid link (ResetPasswordAsync would reject it anyway).
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = decodedToken });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        SetResetPasswordSecurityHeaders();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Don't reveal whether the account exists.
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    // The reset token/email travel in the URL, so this page must never be cached
    // or leaked via Referer to a third-party resource (e.g. an image/script the
    // reset page happens to load), and it shouldn't linger in shared/proxy caches.
    private void SetResetPasswordSecurityHeaders()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
    }
}
