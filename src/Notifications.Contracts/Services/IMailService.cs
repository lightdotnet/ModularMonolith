namespace StarterKit.Notifications.Contracts.Services;

public interface IMailService
{
    Task SendFromSystemAsync(
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default);

    Task SendAsync(
        string from,
        List<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
