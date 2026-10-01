using Light.Extensions.DependencyInjection;
using Light.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Modules.Notifications.Api;
using StarterKit.Modules.Notifications.Application.Common;
using StarterKit.Modules.Notifications.Infrastructure.Mail;
using StarterKit.Modules.Notifications.Infrastructure.Persistence;
using StarterKit.Persistence;

namespace StarterKit.Modules.Notifications;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the notification store (<c>NotificationDbContext</c>, exposed to the use cases as
    /// <see cref="INotificationDbContext"/>) and the
    /// <see cref="INotificationsModuleApi"/> seam. The use cases are mediator handlers, picked up by
    /// the host's mediator assembly scan. The SignalR hub and its push service are registered
    /// separately by <see cref="Infrastructure.SignalR.SignalRModule"/>.
    /// </summary>
    public static IServiceCollection AddNotificationsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddConfiguredDbContext<NotificationDbContext>(
            configuration,
            DbConnectionNames.Default);

        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());

        services.AddScoped<INotificationsModuleApi, NotificationsModuleApi>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IMailService"/> over MailKit from the <c>SmtpMail</c> configuration
    /// section. A missing section, or an empty host/invalid port, fails at startup.
    /// </summary>
    public static IServiceCollection AddSmtpMail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var smtpConfig = configuration.GetSection("SmtpMail").Get<SmtpMailKitOptions>()
            ?? throw new InvalidOperationException("SMTP configuration is missing.");

        services.AddSingleton(smtpConfig);

        services.AddSmtpMailKit(options =>
        {
            options.Host = smtpConfig.Host;
            options.Port = smtpConfig.Port;
            options.UserName = smtpConfig.UserName;
            options.Password = smtpConfig.Password;
            options.UseSsl = smtpConfig.UseSsl;
        });

        services.AddScoped<IMailService, MailService>();

        return services;
    }
}
