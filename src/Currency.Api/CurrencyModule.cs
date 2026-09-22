using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Currencies.Api.Data;
using StarterKit.Currencies.Api.Services;
using StarterKit.Currencies.Contracts.Authorization;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Persistence;

namespace StarterKit.Currencies.Api;

public class CurrencyModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<CurrencyDbContext>(
            configuration,
            DbConnectionNames.Currency);

        services.AddScoped<ICurrencyService, CurrencyService>();

        services.AddSingleton<IPermissionDefinitionProvider, CurrencyPermissionProvider>();

        ShowModuleInfo();
    }
}
