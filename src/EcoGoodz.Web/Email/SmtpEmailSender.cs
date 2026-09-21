using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EcoGoodz.Web.Email;

/// <summary>
/// Sends outbound email via SMTP relay (the GoDaddy-hosted Reports@ecogoodz.com
/// mailbox by default - see SmtpOptions). Replaces the legacy app's raw
/// System.Net.Mail.SmtpClient usage (EcoGoodz_Service/Email.cs) with MailKit, which
/// supports modern TLS negotiation that the old code left to machine-wide config.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.UserName) || string.IsNullOrWhiteSpace(_options.Password))
        {
            // Fail loudly rather than silently dropping mail (e.g. password resets)
            // - an admin needs to know the relay isn't configured yet.
            throw new InvalidOperationException(
                "SMTP credentials are not configured. Set Smtp:UserName and Smtp:Password " +
                "via user-secrets/environment variables before sending email.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromDisplayName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Sent email to {ToEmail} via {Host}", toEmail, _options.Host);
    }
}
