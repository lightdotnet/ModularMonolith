using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Approval.Api.Data;

public class ApprovalContextInitialiser(
    ILogger<ApprovalContextInitialiser> logger,
    ApprovalDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
