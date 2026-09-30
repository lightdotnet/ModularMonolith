namespace StarterKit.Modules.Notifications.Contracts;

/// <summary>
/// Sends email through the SMTP server configured for the Notifications module.
/// </summary>
public interface IMailService
{
    /// <summary>
    /// Sends an email from the system mailbox (the configured SMTP user).
    /// </summary>
    Task SendFromSystemAsync(
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email from the given sender address.
    /// </summary>
    Task SendAsync(
        string from,
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
