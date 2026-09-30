using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;

namespace StarterKit.Modules.Notifications;

public class NotificationsModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationsServices(configuration);

        services.AddSmtpMail(configuration);

        services.AddSingleton<IPermissionDefinitionProvider, NotificationPermissionProvider>();

        ShowModuleInfo();
    }
}
