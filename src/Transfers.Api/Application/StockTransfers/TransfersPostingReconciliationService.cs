using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;

namespace StarterKit.Transfers.Api.Application.StockTransfers;

/// <summary>
/// Periodic backstop for a dispatch or receive that never finished its second commit: finds transfers
/// and receipts stuck in <c>Posting</c> past the grace period and finishes them through
/// <see cref="TransferPosting"/>, whose Inventory calls are idempotent. A dispatch is rolled forward
/// whenever its postings landed and aborted back to Draft only for a real refusal with nothing landed;
/// a receipt is rolled forward when its postings landed and voided only when Inventory deterministically
/// refuses it with nothing landed. Transient failures simply retry on the next tick. Nothing posted to
/// Inventory is ever reversed. Owns no scoped state — a fresh DI scope is created per tick.
/// </summary>
internal sealed class TransfersPostingReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<TransfersPostingReconciliationOptions> options,
    ILogger<TransfersPostingReconciliationService> logger)
    : BackgroundService
{
    private const string PerformedBy = "system:transfers-reconciliation";

    private const int MaxPagesPerTick = 10;

    // In-memory watermarks; a restart simply begins a new pass from the start.
    private readonly TransfersPostingReconciliationState state = new();

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

                var context = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                var clock = scope.ServiceProvider.GetRequiredService<IDateTime>();

                var resolved = await ReconcileOnceAsync(
                    context,
                    inventoryService,
                    clock,
                    opts,
                    state,
                    logger,
                    stoppingToken);

                if (resolved > 0)
                    logger.LogInformation(
                        "Transfers posting reconciliation finished {Count} stuck posting(s).",
                        resolved);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transfers posting reconciliation tick failed.");
            }
        }
    }

    /// <summary>Runs one bounded sweep (transfers, then receipts) and returns how many postings were finished.</summary>
    internal static async Task<int> ReconcileOnceAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        TransfersPostingReconciliationOptions options,
        TransfersPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Only the grace period selects candidates; unlike the Orders sweep, postings are never
        // reversed here, so the Inventory landed-check inside TransferPosting uses "now" (not this
        // cutoff) to see every posting, however recent.
        var cutoff = clock.UtcNow.AddMinutes(-options.StuckAfterMinutes);

        var resolved = await ReconcileDispatchesAsync(
            context,
            inventoryService,
            clock,
            cutoff,
            options,
            state,
            logger,
            cancellationToken);

        resolved += await ReconcileReceiptsAsync(
            context,
            inventoryService,
            clock,
            cutoff,
            options,
            state,
            logger,
            cancellationToken);

        return resolved;
    }

    private static async Task<int> ReconcileDispatchesAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        DateTimeOffset cutoff,
        TransfersPostingReconciliationOptions options,
        TransfersPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var resolved = 0;

        for (var page = 0; page < MaxPagesPerTick; page++)
        {
            var afterId = state.AfterTransferId;

            // Matches the filtered PostingStartedAt index.
            var candidateIds = await context.StockTransfers
                .AsNoTracking()
                .Where(x => x.Id > afterId
                    && x.Status == TransferStatus.Posting
                    && x.PostingStartedAt <= cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken);

            if (candidateIds.Count == 0)
            {
                // Pass finished: start over from the beginning on the next page/tick.
                state.AfterTransferId = 0;
                break;
            }

            // No batch landed-check here: TransferPosting.DispatchAsync repeats the idempotent issue
            // (which returns the original costs when it landed) and only consults Inventory when it fails.
            foreach (var transferId in candidateIds)
            {
                if (await TryFinishDispatchAsync(
                    context,
                    inventoryService,
                    transferId,
                    clock,
                    logger,
                    cancellationToken))
                {
                    resolved++;
                }
            }

            // Advance only once the page has been processed, so a failing load call retries the same
            // page on the next tick instead of skipping it until the pass wraps.
            state.AfterTransferId = candidateIds[^1];

            if (candidateIds.Count < options.BatchSize)
            {
                // Short page means the end of the candidate range was reached.
                state.AfterTransferId = 0;
                break;
            }
        }

        return resolved;
    }

    private static async Task<int> ReconcileReceiptsAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        DateTimeOffset cutoff,
        TransfersPostingReconciliationOptions options,
        TransfersPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var resolved = 0;

        for (var page = 0; page < MaxPagesPerTick; page++)
        {
            var afterId = state.AfterReceiptId;

            var candidateIds = await context.TransferReceipts
                .AsNoTracking()
                .Where(x => x.Id > afterId
                    && x.Status == TransferReceiptStatus.Posting
                    && x.Created <= cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken);

            if (candidateIds.Count == 0)
            {
                state.AfterReceiptId = 0;
                break;
            }

            // Receipts that already landed are applied without calling Inventory again (their costs
            // are frozen on the lines), so a since-deleted destination cannot block them. "now" — not
            // the grace cutoff — because this only ever rolls forward and must see recent postings.
            var landed = (await inventoryService.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.TransferReceipt,
                candidateIds,
                clock.UtcNow,
                cancellationToken)).ToHashSet();

            foreach (var receiptId in candidateIds)
            {
                if (await TryFinishReceiveAsync(
                    context,
                    inventoryService,
                    receiptId,
                    postingsLanded: landed.Contains(receiptId),
                    clock,
                    logger,
                    cancellationToken))
                {
                    resolved++;
                }
            }

            state.AfterReceiptId = candidateIds[^1];

            if (candidateIds.Count < options.BatchSize)
            {
                state.AfterReceiptId = 0;
                break;
            }
        }

        return resolved;
    }

    private static async Task<bool> TryFinishDispatchAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        long transferId,
        IDateTime clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await context.StockTransfers
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(
                    x => x.Id == transferId && x.Status == TransferStatus.Posting,
                    cancellationToken);

            // Finished by the original request in the meantime.
            if (transfer is null)
                return false;

            var outcome = await TransferPosting.DispatchAsync(
                context,
                inventoryService,
                transfer,
                PerformedBy,
                clock,
                cancellationToken);

            switch (outcome.Kind)
            {
                case PostingOutcomeKind.Completed:
                    logger.LogInformation(
                        "Finished stuck dispatch of transfer {TransferId}.",
                        transferId);
                    return true;

                case PostingOutcomeKind.Refused:
                    logger.LogWarning(
                        outcome.Failure,
                        "Aborted stuck dispatch of transfer {TransferId} back to Draft: Inventory refused the issue.",
                        transferId);
                    return true;

                default:
                    logger.LogWarning(
                        outcome.Failure,
                        "Transient Inventory failure while reconciling the dispatch of transfer {TransferId}; it will be retried.",
                        transferId);
                    return false;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // The original request (or another node) finished it first.
            logger.LogDebug(
                "Skipped posting reconciliation for transfer {TransferId}: modified concurrently.",
                transferId);

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to reconcile the dispatch of transfer {TransferId}; it will be retried.",
                transferId);

            return false;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    private static async Task<bool> TryFinishReceiveAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        long receiptId,
        bool postingsLanded,
        IDateTime clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await context.StockTransfers
                .Include(x => x.Lines)
                .Include(x => x.Receipts)
                    .ThenInclude(x => x.Lines)
                .FirstOrDefaultAsync(
                    x => x.Receipts.Any(r => r.Id == receiptId),
                    cancellationToken);

            var receipt = transfer?.Receipts.FirstOrDefault(x => x.Id == receiptId);

            // Gone, or already finished by the original request in the meantime.
            if (transfer is null || receipt is null || receipt.Status != TransferReceiptStatus.Posting)
                return false;

            var outcome = await TransferPosting.ReceiveAsync(
                context,
                inventoryService,
                transfer,
                receipt,
                PerformedBy,
                clock,
                postingsLanded,
                cancellationToken);

            if (outcome.Kind == PostingOutcomeKind.Refused)
            {
                logger.LogWarning(
                    outcome.Failure,
                    "Voided stuck receipt {ReceiptId} of transfer {TransferId}: Inventory refused it and nothing landed.",
                    receiptId,
                    transfer.Id);

                return true;
            }

            logger.LogInformation(
                "Finished stuck receipt {ReceiptId} of transfer {TransferId} (postings landed: {Landed}).",
                receiptId,
                transfer.Id,
                postingsLanded);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogDebug(
                "Skipped posting reconciliation for receipt {ReceiptId}: modified concurrently.",
                receiptId);

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Failed to reconcile receipt {ReceiptId}; it will be retried.",
                receiptId);

            return false;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }
}
