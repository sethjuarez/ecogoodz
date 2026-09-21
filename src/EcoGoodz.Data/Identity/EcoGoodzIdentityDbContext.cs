using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Data.Identity;

/// <summary>
/// Dedicated DbContext for ASP.NET Core Identity (AspNetUsers/AspNetRoles/etc.).
/// Lives in the same physical database as <see cref="EcoGoodzDbContext"/> but is a
/// separate EF Core model, so it never collides with the scaffolded legacy schema
/// and can be regenerated/scaffolded independently.
/// </summary>
public class EcoGoodzIdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public EcoGoodzIdentityDbContext(DbContextOptions<EcoGoodzIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.LegacyUserId).IsUnique();
        });
    }
}
