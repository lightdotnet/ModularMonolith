using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared;

namespace StarterKit.Orders.Api.Application.Orders;

/// <summary>
/// Periodic backstop for a PlaceOrder whose decrement committed in Inventory but whose own order save
/// then failed: finds orders that were never placed (Draft/Cancelled, <c>PlacedAt</c> null) yet still
/// hold an unreversed order-placement posting in Inventory, and asks Inventory to restore that stock.
/// Each order is first fenced with <see cref="Order.MarkStockReconciled"/> so a concurrent
/// Place/Cancel that loaded the same concurrency token loses its save instead of racing the restore.
/// Owns no scoped state — a fresh DI scope is created per tick.
/// </summary>
internal sealed class OrphanedStockReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<OrphanedStockReconciliationOptions> options,
    ILogger<OrphanedStockReconciliationService> logger)
    : BackgroundService
{
    private const string PerformedBy = "system:stock-reconciliation";

    private const int MaxPagesPerTick = 10;

    // In-memory watermark; a restart simply begins a new pass from the start.
    private readonly OrphanedStockReconciliationState state = new();

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

                var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTime>();

                var restored = await ReconcileOnceAsync(
                    context,
                    inventoryService,
                    clock,
                    opts,
                    state,
                    logger,
                    stoppingToken);

                if (restored > 0)
                    logger.LogInformation(
                        "Orphaned stock reconciliation restored stock for {Count} order(s).",
                        restored);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Orphaned stock reconciliation tick failed.");
            }
        }
    }

    internal static async Task<int> ReconcileOnceAsync(
        OrdersDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        OrphanedStockReconciliationOptions options,
        OrphanedStockReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var cutoff = now.AddMinutes(-options.MinAgeMinutes);

        var restored = 0;

        for (var page = 0; page < MaxPagesPerTick; page++)
        {
            var afterId = state.AfterOrderId;

            // Never-placed, old enough orders — matches the filtered (Status, Created) index.
            var candidateIds = await context.Orders
                .AsNoTracking()
                .Where(x => x.Id > afterId
                    && x.PlacedAt == null
                    && (x.Status == OrderStatus.Draft || x.Status == OrderStatus.Cancelled)
                    && x.Created <= cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken);

            if (candidateIds.Count == 0)
            {
                // Pass finished: start over from the beginning on the next page/tick.
                state.AfterOrderId = 0;
                break;
            }

            var withStock = await inventoryService.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.Order,
                candidateIds,
                cutoff,
                cancellationToken);

            if (withStock.Count > 0)
            {
                var orphans = await context.Orders
                    .Where(x => withStock.Contains(x.Id)
                        && x.PlacedAt == null
                        && (x.Status == OrderStatus.Draft || x.Status == OrderStatus.Cancelled))
                    .ToListAsync(cancellationToken);

                foreach (var order in orphans)
                {
                    if (await TryRestoreAsync(context, inventoryService, order, now, cutoff, logger, cancellationToken))
                        restored++;
                }
            }

            // Advance only once the page has been processed, so a failing filter/load call retries the
            // same page on the next tick instead of skipping it until the pass wraps.
            state.AfterOrderId = candidateIds[^1];

            if (candidateIds.Count < options.BatchSize)
            {
                // Short page means the end of the candidate range was reached.
                state.AfterOrderId = 0;
                break;
            }
        }

        return restored;
    }

    private static async Task<bool> TryRestoreAsync(
        OrdersDbContext context,
        IInventoryService inventoryService,
        Order order,
        DateTimeOffset now,
        DateTimeOffset cutoff,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            order.MarkStockReconciled(now);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Place/Cancel won the race; the order is no longer an orphan candidate, or will be
            // picked up again on a later tick.
            logger.LogDebug(
                "Skipped stock reconciliation for order {OrderId}: modified concurrently.",
                order.Id);

            context.Entry(order).State = EntityState.Detached;

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to fence order {OrderId} for stock reconciliation; it will be retried.",
                order.Id);

            context.Entry(order).State = EntityState.Detached;

            return false;
        }

        try
        {
            await inventoryService.RestoreForOrderAsync(
                order.Id,
                PerformedBy,
                cancellationToken,
                postedBefore: cutoff);

            logger.LogInformation(
                "Restored orphaned stock for order {OrderId}.",
                order.Id);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to restore orphaned stock for order {OrderId}; it will be retried.",
                order.Id);

            return false;
        }
    }
}
