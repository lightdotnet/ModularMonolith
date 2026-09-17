using Light.Smtp;
using StarterKit.Notifications.Contracts.Services;

namespace StarterKit.Notifications.Api.Services;

internal class MailService(
    ISmtpMailSender smtpMailSender,
    SmtpMailKitOptions options) : IMailService
{
    public Task SendFromSystemAsync(
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // fake system email address for now, we can change it later
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
