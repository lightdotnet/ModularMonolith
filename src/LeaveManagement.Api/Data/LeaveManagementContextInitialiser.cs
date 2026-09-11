using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.LeaveManagement.Api.Data;

public class LeaveManagementContextInitialiser(
    ILogger<LeaveManagementContextInitialiser> logger,
    LeaveManagementDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
