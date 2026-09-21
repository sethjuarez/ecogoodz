using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Web.Email;
using EcoGoodz.Web.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EcoGoodz")
    ?? throw new InvalidOperationException("Connection string 'EcoGoodz' not found.");

// Business/legacy schema (Buyer, Supplier, Product, Load, etc.) - scaffolded from the
// restored & normalized database.
builder.Services.AddDbContext<EcoGoodzDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity schema (AspNetUsers/AspNetRoles/etc.) - separate from the legacy
// User table, linked via ApplicationUser.LegacyUserId. See EcoGoodzIdentityDbContext for why.
builder.Services.AddDbContext<EcoGoodzIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        // Reasonable modern defaults; legacy app had no password policy at all.
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Lockout.MaxFailedAccessAttempts = 10;
        // Legacy data was checked (all 18 migrated users have unique, non-empty
        // emails) before enabling this - see docs/security-review.md. Without it,
        // FindByEmailAsync during ForgotPassword/ResetPassword could silently
        // resolve to the wrong account if a future data import introduced a
        // duplicate/shared email.
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<EcoGoodzIdentityDbContext>()
    .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    // Reset tokens, passwords, and the session cookie itself are only safe on the
    // wire if the cookie can never be sent unencrypted. Never weaken this even in
    // dev - HTTPS is used locally too (see launchSettings.json).
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
});

// Plesk/IIS hosts this app out-of-process behind the ASP.NET Core Module, which
// reverse-proxies from IIS (on localhost) to Kestrel. Without this, Request.Scheme
// would report "http" even when the public-facing request was HTTPS, which would
// both generate insecure reset links (Url.Action(..., Request.Scheme)) and prevent
// the Secure cookie policy above from ever attaching the auth cookie. The default
// KnownProxies/KnownNetworks (loopback only) match this exact on-box topology.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ForcePasswordChangeFilter>();
});

// Outbound email - reuses the legacy app's GoDaddy SMTP relay (Reports@ecogoodz.com
// via smtpout.secureserver.net) rather than standing up a new provider. Credentials
// must come from user-secrets/environment, not appsettings - see SmtpOptions.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Per-IP rate limiting on the unauthenticated auth endpoints (Login/ForgotPassword/
// ResetPassword). These have no other abuse protection: Identity's lockout only
// engages after a valid *username* is found, so login enumeration and reset-email
// flooding are otherwise unbounded. A fixed window keyed on remote IP is enough for
// a low-traffic internal-staff app like this - not meant to defend against a
// distributed attacker, just casual abuse/scripted flooding.
const string AuthRateLimitPolicy = "auth";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
    await LegacyUserMigrator.MigrateAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Must run before UseHttpsRedirection/auth so Request.Scheme and the Secure cookie
// policy see the real (public-facing) scheme rather than the internal proxy hop.
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

// Dev-only convenience: skips the login screen for local runs by silently signing
// in as the configured user on every request that isn't already authenticated.
// Same belt-and-suspenders gating as DevTools:ShowResetLinkInResponse - requires
// both the config flag AND IWebHostEnvironment.IsDevelopment(), so it can never
// fire against a deployed appsettings even if the flag were left set.
var devAutoLoginEmail = builder.Configuration["DevTools:AutoLoginEmail"];
if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(devAutoLoginEmail))
{
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            var devUser = await userManager.FindByEmailAsync(devAutoLoginEmail);
            if (devUser is not null)
            {
                await signInManager.SignInAsync(devUser, isPersistent: true);
                context.User = await signInManager.CreateUserPrincipalAsync(devUser);
            }
        }

        await next();
    });
}

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
