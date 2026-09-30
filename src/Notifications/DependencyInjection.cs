using Light.Extensions.DependencyInjection;
using Light.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Modules.Notifications.Api;
using StarterKit.Modules.Notifications.Persistence;
using StarterKit.Modules.Notifications.Services;
using StarterKit.Persistence;

namespace StarterKit.Modules.Notifications;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the notification store (<c>NotificationDbContext</c>) and the
    /// <see cref="INotificationsModuleApi"/> seam. The use cases are mediator handlers, picked up by
    /// the host's mediator assembly scan. The SignalR hub and its push service are registered
    /// separately by <see cref="SignalR.SignalRModule"/>.
    /// </summary>
    public static IServiceCollection AddNotificationsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddConfiguredDbContext<NotificationDbContext>(
            configuration,
            DbConnectionNames.Default);

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
