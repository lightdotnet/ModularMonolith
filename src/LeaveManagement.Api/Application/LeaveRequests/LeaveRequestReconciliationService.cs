using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// Periodic backstop for <c>ApprovalFinalizedIntegrationEvent</c>: sweeps locally <c>Pending</c>
/// leave requests that carry an approval workflow and pulls their current status from Approval,
/// covering any in-process delivery that was missed or failed. Owns no scoped state — a fresh DI
/// scope is created per tick.
/// </summary>
internal sealed class LeaveRequestReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<LeaveReconciliationOptions> options,
    ILogger<LeaveRequestReconciliationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;

        if (!opts.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(opts.IntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
                var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalService>();

                var changed = await ReconcileOnceAsync(
                    context,
                    approvalService,
                    opts.BatchSize,
                    stoppingToken);

                if (changed > 0)
                    logger.LogInformation(
                        "Leave request reconciliation updated {Count} row(s).",
                        changed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Leave request reconciliation tick failed.");
            }
        }
    }

    internal static async Task<int> ReconcileOnceAsync(
        LeaveManagementDbContext context,
        IApprovalService approvalService,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var rows = await context.LeaveRequests
            .Where(x => x.Status == LeaveRequestStatus.Pending && x.ApprovalRequestId != null)
            .OrderBy(x => x.Created)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return 0;

        var views = await approvalService.GetStatusesByRequestAsync(
            LeaveRequestStatusMap.RequestType,
            rows.Select(x => x.Id).ToList(),
            cancellationToken);

        var changed = 0;

        foreach (var row in rows)
        {
            if (!views.TryGetValue(row.Id, out var view))
                continue;

            var mapped = LeaveRequestStatusMap.MapStatus(view.Status);

            if (mapped != row.Status)
            {
                row.Status = mapped;
                changed++;
            }
        }

        if (changed > 0)
            await context.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
