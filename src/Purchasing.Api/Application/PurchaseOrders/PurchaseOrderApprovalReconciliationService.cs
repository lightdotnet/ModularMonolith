using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarterKit.Approval.Contracts.Services;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders;

/// <summary>
/// Periodic backstop for <c>ApprovalFinalizedIntegrationEvent</c>: sweeps purchase orders still
/// <c>PendingApproval</c> that carry an approval workflow and pulls their current status from Approval,
/// covering any in-process delivery that was missed or failed. Owns no scoped state — a fresh DI scope
/// is created per tick. Mirrors LeaveManagement's <c>LeaveRequestReconciliationService</c>.
/// </summary>
internal sealed class PurchaseOrderApprovalReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<PurchaseOrderApprovalReconciliationOptions> options,
    ILogger<PurchaseOrderApprovalReconciliationService> logger)
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

                var context = scope.ServiceProvider.GetRequiredService<PurchasingDbContext>();
                var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalService>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTime>();

                var changed = await ReconcileOnceAsync(
                    context,
                    approvalService,
                    clock,
                    opts.BatchSize,
                    stoppingToken);

                if (changed > 0)
                    logger.LogInformation(
                        "Purchase order approval reconciliation updated {Count} row(s).",
                        changed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Purchase order approval reconciliation tick failed.");
            }
        }
    }

    internal static async Task<int> ReconcileOnceAsync(
        PurchasingDbContext context,
        IApprovalService approvalService,
        IDateTime clock,
        int batchSize,
        CancellationToken cancellationToken)
    {
        // Known limitation inherited from LeaveManagement's sweep: the oldest PendingApproval rows are
        // taken first, so a backlog larger than the batch that Approval keeps reporting as still pending
        // could starve newer rows of the backstop. The integration event is the primary path; behaviour is
        // intentionally unchanged.
        var rows = await context.PurchaseOrders
            .Where(new ReconcilablePurchaseOrdersSpec())
            .OrderBy(x => x.Created)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return 0;

        // Look up by the stored approval request id, not the newest request for (type, order id): a
        // request forged for the same order can then never shadow the genuine workflow.
        var views = await approvalService.GetStatusesAsync(
            rows.Select(x => x.ApprovalRequestId!).ToList(),
            cancellationToken);

        var now = clock.UtcNow;
        var changed = 0;

        foreach (var row in rows)
        {
            if (!views.TryGetValue(row.ApprovalRequestId!, out var view))
                continue;

            // TryApplyOutcome (and, through it, ApplyApprovalOutcome) is the sole authority on ignoring a
            // superseded workflow or an already-decided order. The sweep batches one save after the loop.
            if (PurchaseOrderApprovalCoordinator.TryApplyOutcome(row, view, now))
                changed++;
        }

        if (changed > 0)
            await context.SaveChangesAsync(cancellationToken);

        return changed;
    }
}
