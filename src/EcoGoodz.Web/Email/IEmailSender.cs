namespace EcoGoodz.Web.Email;

/// <summary>
/// Abstraction over outbound transactional email so the SMTP relay/provider can be
/// swapped later (GoDaddy mailbox today, SendGrid/etc. later) without touching callers.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
