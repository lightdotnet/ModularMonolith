using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Infrastructure.Modularity;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Contracts.Authorization;
using StarterKit.Persistence;

namespace StarterKit.LeaveManagement.Api;

public class LeaveManagementModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<LeaveManagementDbContext>(
            configuration,
            DbConnectionNames.LeaveManagement);

        services.AddSingleton<IPermissionDefinitionProvider, LeaveManagementPermissionProvider>();

        services.AddScoped<LeaveRequestApprovalCoordinator>();

        LeaveRequestMappingConfig.Register();

        services.AddOptions<LeaveReconciliationOptions>()
            .BindConfiguration("LeaveManagement:Reconciliation")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<LeaveRequestReconciliationService>();

        ShowModuleInfo();
    }
}
