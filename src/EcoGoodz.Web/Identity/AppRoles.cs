namespace EcoGoodz.Web.Identity;

/// <summary>
/// The 4 roles carried over from the legacy [Role] table (Id 1-4), now backed by
/// ASP.NET Core Identity roles instead of a hand-rolled Session-based check.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string AccountManager = "AccountManager";
    public const string Accounting = "Accounting";
    public const string LoadManager = "Load Manager";

    public static readonly string[] All = { Admin, AccountManager, Accounting, LoadManager };
}
