namespace EcoGoodz.Web.Email;

/// <summary>
/// SMTP relay settings, bound from the "Smtp" configuration section. This mirrors
/// the legacy app's &lt;system.net&gt;&lt;mailSettings&gt; setup (Web.config), which
/// relayed through the GoDaddy-hosted Reports@ecogoodz.com mailbox via
/// smtpout.secureserver.net. Credentials are intentionally NOT given defaults here -
/// they must come from user-secrets/environment/Key Vault, never checked into
/// appsettings.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtpout.secureserver.net";

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "Reports@ecogoodz.com";

    public string FromDisplayName { get; set; } = "EcoGoodz";
}
