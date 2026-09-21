using Microsoft.AspNetCore.Identity;

namespace EcoGoodz.Data.Identity;

/// <summary>
/// Login/authentication identity for a staff member. Kept separate from the legacy
/// <see cref="Models.User"/> business table (which is referenced by dozens of FKs
/// throughout the schema and continues to hold names, roles-for-display, etc.).
/// <see cref="LegacyUserId"/> links the two records 1:1.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public int LegacyUserId { get; set; }

    /// <summary>
    /// True until the user completes a one-time password reset after the migration
    /// from the legacy plaintext-password system. Enforced at login.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;
}
