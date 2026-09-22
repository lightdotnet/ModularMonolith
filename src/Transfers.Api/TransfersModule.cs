using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Persistence;
using StarterKit.Transfers.Api.Application.StockTransfers;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Services;
using StarterKit.Transfers.Contracts.Authorization;

namespace StarterKit.Transfers.Api;

public class TransfersModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<TransfersDbContext>(
            configuration,
            DbConnectionNames.Transfers);

        services.AddScoped<TransferLocationResolver>();

        services.AddSingleton<IPermissionDefinitionProvider, TransfersPermissionProvider>();

        // Every default lives in the options class; no configuration section is required.
        services.AddOptions<TransfersPostingReconciliationOptions>()
            .BindConfiguration("Transfers:PostingReconciliation")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<TransfersPostingReconciliationService>();

        ShowModuleInfo();
    }
}
