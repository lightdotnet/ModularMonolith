using Microsoft.Extensions.Logging;
using StarterKit.Identity.Contracts.Users;
using StarterKit.Notifications.Contracts.Services;

namespace StarterKit.Notifications.Api.Application.Users.EventHandlers;

internal class ExternalUserProvisionedIntegrationEventHandler(
    IMailService mailService,
    ILogger<ExternalUserProvisionedIntegrationEventHandler> logger)
    : INotificationHandler<ExternalUserProvisionedIntegrationEvent>
{
    public async Task Handle(ExternalUserProvisionedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("External user provisioned: {UserId}, {Email}, {Provider}",
            notification.UserId,
            notification.Email,
            notification.Provider);

        if (string.IsNullOrEmpty(notification.Email))
            return;

        var displayName = $"{notification.FirstName} {notification.LastName}".Trim();
        if (string.IsNullOrEmpty(displayName))
            displayName = notification.Email;

        try
        {
            await mailService.SendFromSystemAsync(
                [notification.Email],
                "Welcome to the system",
                GenerateWelcomeEmailBody(displayName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", notification.Email);
        }
    }

    private static string GenerateWelcomeEmailBody(string user)
    {
        var welcomeEmail = "" +
            $"Dear {user}," +
            "<br><br>" +
            "Your account has been created and is ready to use." +
            "<br>" +
            "Sign in with your Microsoft work or school account — no separate username or password is needed." +
            "<br><br>" +
            "If you have any questions or need further assistance, feel free to reach out." +
            "<br><br>" +
            "Warm regards!";

        return welcomeEmail;
    }
}
