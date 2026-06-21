using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ProjectResourceManagement.Server.Services.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning(
                "SMTP is not configured (Enabled={Enabled}, Host={Host}, FromAddress={FromAddress}); email to {Recipient} was not sent.",
                settings.Enabled,
                settings.Host,
                settings.FromAddress,
                to);
            return;
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            logger.LogWarning("Cannot send email with an empty recipient.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromDisplayName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to.Trim()));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, GetSecureSocketOptions(settings), cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {Recipient}, Subject={Subject}", to, subject);
    }

    private static SecureSocketOptions GetSecureSocketOptions(SmtpSettings settings)
    {
        if (!settings.UseSsl)
        {
            return SecureSocketOptions.None;
        }

        return settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}
