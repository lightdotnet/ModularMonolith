using Light.Smtp;

namespace StarterKit.Modules.Notifications.Infrastructure.Mail;

internal sealed class MailService(
    ISmtpMailSender smtpMailSender,
    SmtpMailKitOptions options)
    : IMailService
{
    public Task SendFromSystemAsync(
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // The configured SMTP user doubles as the system sender address.
        var systemEmail = options.UserName;

        return smtpMailSender.SendAsync(
            systemEmail,
            "System",
            recipients,
            subject,
            body,
            cancellationToken: cancellationToken);
    }

    public Task SendAsync(
        string from,
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        return smtpMailSender.SendAsync(
            from,
            from,
            recipients,
            subject,
            body,
            cancellationToken: cancellationToken);
    }
}
