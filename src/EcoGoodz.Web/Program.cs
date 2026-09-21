using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Web.Email;
using EcoGoodz.Web.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Lockout.MaxFailedAccessAttempts = 10;
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
