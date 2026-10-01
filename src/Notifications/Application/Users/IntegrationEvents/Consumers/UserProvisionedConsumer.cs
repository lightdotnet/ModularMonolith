using StarterKit.EventBusMassTransitRabbitMQ;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;

namespace StarterKit.Modules.Notifications.Application.Users.IntegrationEvents.Consumers;

/// <summary>
/// Sends the welcome email when the Identity module provisions a user. The mail body depends on
/// how the user was provisioned: an administrator-created account, or one created just-in-time on
/// the first external sign-in.
/// </summary>
/// <remarks>
/// The event contract asks consumers to be idempotent. A welcome mail is fire-and-forget: no
/// delivery record is kept, so a redelivered event can send the mail twice. That is accepted
/// rather than adding dedup storage for it. Send failures are logged and swallowed so a mail
/// outage does not push the message through retries into the error queue.
/// </remarks>
internal sealed class UserProvisionedConsumer(
    IMailService mailService,
    ILogger<UserProvisionedConsumer> logger)
    : AppConsumer<UserProvisionedIntegrationEvent>(logger)
{
    public override async Task Handle(UserProvisionedIntegrationEvent message)
    {
        logger.LogInformation(
            "User provisioned: {UserId}, {UserName}, {Email}, {Source}",
            message.UserId,
            message.UserName,
            message.Email,
            message.Source);

        if (string.IsNullOrEmpty(message.Email))
            return;

        var (subject, body) = message.Source switch
        {
            ProvisioningSource.ExternalLogin => (
                "Welcome to the system",
                GenerateExternalLoginWelcomeBody(GetDisplayName(message))),
            _ => (
                "Welcome to system!",
                GenerateWelcomeBody(string.IsNullOrEmpty(message.UserName) ? message.Email : message.UserName)),
        };

        try
        {
            await mailService.SendFromSystemAsync(
                [message.Email],
                subject,
                body);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send welcome email to {Email}",
                message.Email);
        }
    }

    private static string GetDisplayName(UserProvisionedIntegrationEvent message)
    {
        var displayName = $"{message.FirstName} {message.LastName}".Trim();

        return string.IsNullOrEmpty(displayName) ? message.Email : displayName;
    }

    private static string GenerateWelcomeBody(string user) =>
        $"Dear {user}," +
        "<br><br>" +
        "We are pleased to provide you with your login credentials for our system. Please find the details below:" +
        "<br>" +
        $"- <b>Username</b>: {user}" +
        "<br>" +
        "- <b>Password</b>: ***is_your_company_account_p@ssword***" +
        "<br><br>" +
        "If you have any questions or need further assistance, feel free to reach out." +
        "<br><br>" +
        "Warm regards!";

    private static string GenerateExternalLoginWelcomeBody(string user) =>
        $"Dear {user}," +
        "<br><br>" +
        "Your account has been created and is ready to use." +
        "<br>" +
        "Sign in with your Microsoft work or school account — no separate username or password is needed." +
        "<br><br>" +
        "If you have any questions or need further assistance, feel free to reach out." +
        "<br><br>" +
        "Warm regards!";
}

internal sealed class UserProvisionedConsumerDefinition()
    : AppConsumerDefinition<UserProvisionedIntegrationEvent, UserProvisionedConsumer>(NotificationsModuleConsumer.EndpointNamePrefix);