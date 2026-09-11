using Light.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Approval.Api.Data;
using StarterKit.Approval.Api.Services;
using StarterKit.Approval.Contracts.Authorization;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Infrastructure.Modularity;
using StarterKit.Persistence;

namespace StarterKit.Approval.Api;

public class ApprovalModule : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddConfiguredDbContext<ApprovalDbContext>(
            configuration,
            DbConnectionNames.Approval);

        services.AddScoped<IApprovalService, ApprovalService>();

        services.AddSingleton<IPermissionDefinitionProvider, ApprovalPermissionProvider>();

        ShowModuleInfo();
    }
}
