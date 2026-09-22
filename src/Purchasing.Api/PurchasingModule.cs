using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Persistence;
using StarterKit.Purchasing.Api.Application.Posting;
using StarterKit.Purchasing.Api.Application.PurchaseOrders;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Services;
using StarterKit.Purchasing.Contracts.Authorization;

namespace StarterKit.Purchasing.Api;

public class PurchasingModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<PurchasingDbContext>(
            configuration,
            DbConnectionNames.Purchasing);

        services.AddScoped<ReceivingLocationResolver>();

        services.AddScoped<PurchaseOrderApprovalCoordinator>();

        services.AddSingleton<IPermissionDefinitionProvider, PurchasingPermissionProvider>();

        // Every default lives in the options classes; no configuration section is required.
        services.AddOptions<PurchasingPostingReconciliationOptions>()
            .BindConfiguration("Purchasing:PostingReconciliation")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<PurchasingPostingReconciliationService>();

        services.AddOptions<PurchaseOrderApprovalReconciliationOptions>()
            .BindConfiguration("Purchasing:ApprovalReconciliation")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<PurchaseOrderApprovalReconciliationService>();

        ShowModuleInfo();
    }
}
