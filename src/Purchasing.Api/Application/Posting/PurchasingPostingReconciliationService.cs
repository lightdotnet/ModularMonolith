using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.Posting;

/// <summary>
/// Periodic backstop for a goods receipt or purchase return that never finished its second commit: finds
/// receipts and returns stuck in <c>Posting</c> past the grace period and rolls them forward. Receipts
/// that Inventory already holds postings for (<see cref="IInventoryService.FilterSourceIdsWithUnreversedPostingsAsync"/>)
/// are applied without calling Inventory again; every other call repeats the idempotent Inventory
/// request, which returns the originally posted results when it had landed and posts it when it had not.
/// A return whose postings never landed and is now refused for shortage is aborted back to Draft, and a
/// receipt Inventory refuses outright is voided (a receipt cannot fail on stock). Nothing is ever
/// reversed. Owns no scoped state — a fresh DI scope is created per tick. Mirrors
/// <c>TransfersPostingReconciliationService</c>.
/// </summary>
internal sealed class PurchasingPostingReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<PurchasingPostingReconciliationOptions> options,
    ILogger<PurchasingPostingReconciliationService> logger)
    : BackgroundService
{
    private const int MaxPagesPerTick = 10;

    private const int PoisonAfterGraceMultiples = 10;

    // In-memory watermarks; a restart simply begins a new pass from the start.
    private readonly PurchasingPostingReconciliationState state = new();

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
                        "Purchasing posting reconciliation finished {Count} stuck posting(s).",
                        resolved);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Purchasing posting reconciliation tick failed.");
            }
        }
    }

    /// <summary>Runs one bounded sweep (goods receipts, then purchase returns) and returns how many postings were finished.</summary>
    internal static async Task<int> ReconcileOnceAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        PurchasingPostingReconciliationOptions options,
        PurchasingPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Only the grace period selects candidates; postings are never reversed here, so the Inventory
        // landed-checks use "now" (not this cutoff) to see every posting, however recent.
        var cutoff = clock.UtcNow.AddMinutes(-options.StuckAfterMinutes);

        // A document still Posting this long is not a transient hiccup (the sweep has retried it many
        // times), so its failures are logged at Error to make it visible.
        var poisonCutoff = clock.UtcNow.AddMinutes(-PoisonAfterGraceMultiples * options.StuckAfterMinutes);

        var resolved = await ReconcileReceiptsAsync(
            context,
            inventoryService,
            clock,
            cutoff,
            poisonCutoff,
            options,
            state,
            logger,
            cancellationToken);

        resolved += await ReconcileReturnsAsync(
            context,
            inventoryService,
            clock,
            cutoff,
            poisonCutoff,
            options,
            state,
            logger,
            cancellationToken);

        return resolved;
    }

    private static async Task<int> ReconcileReceiptsAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        DateTimeOffset cutoff,
        DateTimeOffset poisonCutoff,
        PurchasingPostingReconciliationOptions options,
        PurchasingPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var resolved = 0;

        for (var page = 0; page < MaxPagesPerTick; page++)
        {
            var afterId = state.AfterReceiptId;

            var candidateIds = await context.GoodsReceipts
                .AsNoTracking()
                .Where(x => x.Id > afterId
                    && x.Status == GoodsReceiptStatus.Posting
                    && x.Created <= cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken);

            if (candidateIds.Count == 0)
            {
                // Pass finished: start over from the beginning on the next tick.
                state.AfterReceiptId = 0;
                break;
            }

            // Receipts that already landed are applied without calling Inventory again (their costs are
            // frozen on the lines), so a since-deleted location cannot block them.
            var landed = (await inventoryService.FilterSourceIdsWithUnreversedPostingsAsync(
                StockSourceType.GoodsReceipt,
                candidateIds,
                clock.UtcNow,
                cancellationToken)).ToHashSet();

            foreach (var receiptId in candidateIds)
            {
                if (await TryFinishReceiptAsync(
                    context,
                    inventoryService,
                    receiptId,
                    postingsLanded: landed.Contains(receiptId),
                    poisonCutoff,
                    clock,
                    logger,
                    cancellationToken))
                {
                    resolved++;
                }
            }

            // Advance only once the page has been processed, so a failing filter/load call retries the
            // same page on the next tick instead of skipping it until the pass wraps.
            state.AfterReceiptId = candidateIds[^1];

            if (candidateIds.Count < options.BatchSize)
            {
                // Short page means the end of the candidate range was reached.
                state.AfterReceiptId = 0;
                break;
            }
        }

        return resolved;
    }

    private static async Task<int> ReconcileReturnsAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        IDateTime clock,
        DateTimeOffset cutoff,
        DateTimeOffset poisonCutoff,
        PurchasingPostingReconciliationOptions options,
        PurchasingPostingReconciliationState state,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var resolved = 0;

        for (var page = 0; page < MaxPagesPerTick; page++)
        {
            var afterId = state.AfterReturnId;

            // Matches the filtered PostingStartedAt index.
            var candidateIds = await context.PurchaseReturns
                .AsNoTracking()
                .Where(x => x.Id > afterId
                    && x.Status == PurchaseReturnStatus.Posting
                    && x.PostingStartedAt <= cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(options.BatchSize)
                .ToListAsync(cancellationToken);

            if (candidateIds.Count == 0)
            {
                state.AfterReturnId = 0;
                break;
            }

            // No batch landed-check here: PurchasingPosting.PostReturnAsync repeats the idempotent issue
            // (which returns the original results when it landed) and only consults Inventory when it fails.
            foreach (var returnId in candidateIds)
            {
                if (await TryFinishReturnAsync(
                    context,
                    inventoryService,
                    returnId,
                    poisonCutoff,
                    clock,
                    logger,
                    cancellationToken))
                {
                    resolved++;
                }
            }

            state.AfterReturnId = candidateIds[^1];

            if (candidateIds.Count < options.BatchSize)
            {
                state.AfterReturnId = 0;
                break;
            }
        }

        return resolved;
    }

    private static async Task<bool> TryFinishReceiptAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        long receiptId,
        bool postingsLanded,
        DateTimeOffset poisonCutoff,
        IDateTime clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var isPoison = false;
        var number = receiptId.ToString();

        try
        {
            var receipt = await context.GoodsReceipts
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(
                    x => x.Id == receiptId && x.Status == GoodsReceiptStatus.Posting,
                    cancellationToken);

            // Finished by the original request in the meantime.
            if (receipt is null)
                return false;

            isPoison = receipt.Created <= poisonCutoff;
            number = receipt.ReceiptNumber.Value;

            var outcome = await PurchasingPosting.ReceiveAsync(
                context,
                inventoryService,
                receipt,
                clock,
                postingsLanded,
                logger,
                cancellationToken);

            if (outcome.Kind == PostingOutcomeKind.Refused)
            {
                logger.LogWarning(
                    outcome.Failure,
                    "Voided stuck goods receipt {ReceiptId} of purchase order {PurchaseOrderId}: Inventory refused it and nothing landed.",
                    receiptId,
                    receipt.PurchaseOrderId);

                return true;
            }

            logger.LogInformation(
                "Finished stuck goods receipt {ReceiptId} of purchase order {PurchaseOrderId} (postings landed: {Landed}).",
                receiptId,
                receipt.PurchaseOrderId,
                postingsLanded);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The original request (or another node) finished it first.
            logger.LogDebug(
                "Skipped posting reconciliation for goods receipt {ReceiptId}: modified concurrently.",
                receiptId);

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailure(
                logger,
                ex,
                isPoison,
                "goods receipt",
                receiptId,
                number);

            return false;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// A failure is a warning while the sweep is still expected to succeed on a later tick, and an error
    /// once the document has been Posting for many grace periods — at that point something deterministic
    /// (a domain guard in the completion step, bad data) is more likely than a transient fault, and a
    /// person has to look at it.
    /// </summary>
    private static void LogFailure(
        ILogger logger,
        Exception? exception,
        bool isPoison,
        string documentKind,
        long documentId,
        string documentNumber)
    {
        if (isPoison)
        {
            logger.LogError(
                exception,
                "Stuck {DocumentKind} {DocumentNumber} (id {DocumentId}) still cannot be finished after many attempts; manual attention is needed.",
                documentKind,
                documentNumber,
                documentId);

            return;
        }

        logger.LogWarning(
            exception,
            "Failed to reconcile {DocumentKind} {DocumentNumber} (id {DocumentId}); it will be retried.",
            documentKind,
            documentNumber,
            documentId);
    }

    private static async Task<bool> TryFinishReturnAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        long returnId,
        DateTimeOffset poisonCutoff,
        IDateTime clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var isPoison = false;
        var number = returnId.ToString();

        try
        {
            var purchaseReturn = await context.PurchaseReturns
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(
                    x => x.Id == returnId && x.Status == PurchaseReturnStatus.Posting,
                    cancellationToken);

            // Finished by the original request in the meantime.
            if (purchaseReturn is null)
                return false;

            isPoison = purchaseReturn.PostingStartedAt <= poisonCutoff;
            number = purchaseReturn.ReturnNumber.Value;

            var outcome = await PurchasingPosting.PostReturnAsync(
                context,
                inventoryService,
                purchaseReturn,
                clock,
                logger,
                cancellationToken);

            switch (outcome.Kind)
            {
                case PostingOutcomeKind.Completed:
                    logger.LogInformation(
                        "Finished stuck purchase return {ReturnId}.",
                        returnId);
                    return true;

                case PostingOutcomeKind.Refused:
                    logger.LogWarning(
                        outcome.Failure,
                        "Aborted stuck purchase return {ReturnId} back to Draft: Inventory refused the issue.",
                        returnId);
                    return true;

                default:
                    LogFailure(
                        logger,
                        outcome.Failure,
                        isPoison,
                        "purchase return",
                        returnId,
                        number);
                    return false;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogDebug(
                "Skipped posting reconciliation for purchase return {ReturnId}: modified concurrently.",
                returnId);

            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailure(
                logger,
                ex,
                isPoison,
                "purchase return",
                returnId,
                number);

            return false;
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }
}
