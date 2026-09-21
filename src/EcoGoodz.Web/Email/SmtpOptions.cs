namespace EcoGoodz.Web.Email;

/// <summary>
/// SMTP relay settings, bound from the "Smtp" configuration section. Sends via
/// Amazon SES's SMTP interface (see docs/deployment.md) rather than the legacy
/// app's GoDaddy-hosted Reports@ecogoodz.com mailbox (&lt;system.net&gt;&lt;mailSettings&gt;
/// in the old Web.config) - that mailbox turned out to be Microsoft 365 (via
/// GoDaddy's reseller portal), not a GoDaddy relay, and nobody could safely get
/// at its credentials without touching a real staff member's mailbox. SES gives
/// a dedicated, app-owned sender identity instead. Credentials are intentionally
/// NOT given defaults here - they must come from user-secrets/environment/Key
/// Vault, never checked into appsettings.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "email-smtp.us-east-1.amazonaws.com";

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@ecogoodz.com";

    public string FromDisplayName { get; set; } = "EcoGoodz";

    /// <summary>
    /// SES tenant name (see docs/deployment.md). When set, sent as the
    /// X-SES-TENANT header so this app's sending reputation is isolated from
    /// any other project sharing the same SES account. Optional - if unset,
    /// no header is added and the message is sent at the account level.
    /// </summary>
    public string? TenantName { get; set; }
}
