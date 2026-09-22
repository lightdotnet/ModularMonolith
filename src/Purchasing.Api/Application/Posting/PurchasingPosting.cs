using Light.Exceptions;
using Microsoft.Extensions.Logging;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;
using StarterKit.Shared;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Application.Posting;

internal enum PostingOutcomeKind
{
    /// <summary>Posted to Inventory and committed (receipt applied / return posted).</summary>
    Completed,

    /// <summary>Inventory refused and the refusal was recorded (receipt voided / return aborted to Draft).</summary>
    Refused,

    /// <summary>Failed for a transient reason with nothing landed; the document stays in Posting for a retry.</summary>
    Pending,
}

/// <summary><see cref="Failure"/> is the Inventory exception for <see cref="PostingOutcomeKind.Refused"/>/<see cref="PostingOutcomeKind.Pending"/>.</summary>
internal sealed record PostingOutcome(
    PostingOutcomeKind Kind,
    Exception? Failure = null);

/// <summary>
/// The second half of a goods receipt / purchase return: post to Inventory, then commit the outcome to
/// Purchasing. Shared by the command handlers and the reconciliation sweep so both paths behave
/// identically. Every Inventory call here is idempotent per line, so re-running it (a retry, or the
/// sweep finishing a request whose process died) returns the originally posted results instead of
/// posting twice. Nothing posted to Inventory is ever reversed. The Inventory movement is attributed to
/// the user recorded on the document (<c>ReceivedByUserId</c>/<c>PostedBy</c>), also when the sweep
/// finishes it. Same shape as Transfers' <c>TransferPosting</c>.
/// </summary>
internal static class PurchasingPosting
{
    /// <summary>Only for documents without a recorded user (none are expected).</summary>
    internal const string SystemUser = "system:purchasing-reconciliation";

    private const int MaxCommitAttempts = 3;

    private const string VoidReason = "Inventory refused the receipt";

    internal const string StockRecordedMessage =
        "The stock movement was recorded, but the document could not be finalized right now; it will be completed automatically shortly.";

    /// <summary>
    /// Receives the receipt's stock at each line's frozen cost, then — in one commit — marks the receipt
    /// posted and applies its quantities to the purchase order. Positive movements cannot be refused for
    /// shortage, so the only deterministic refusal is a <see cref="ValidationException"/> (receiving
    /// location gone, invalid cost): if nothing landed the receipt is voided (with a short fixed reason;
    /// the detail is logged) so it stops blocking the order; if postings landed the receipt is applied
    /// anyway (its costs are frozen on the lines). Any other failure — including a
    /// <see cref="ConflictException"/>, which for a receive is only ever the transient
    /// concurrent-modification error — propagates and leaves the receipt in Posting for a retry.
    /// Once the stock has landed, commit 2 is retried against freshly loaded state if a concurrent write
    /// rotated the order's token; if that keeps failing, a conflict that says the movement was recorded
    /// is thrown and the sweep finishes the receipt.
    /// </summary>
    /// <param name="postingsAlreadyLanded">The caller already knows Inventory holds this receipt's postings, so the call is skipped.</param>
    public static async Task<PostingOutcome> ReceiveAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        GoodsReceipt receipt,
        IDateTime clock,
        bool postingsAlreadyLanded,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var receiptId = receipt.Id;
        var purchaseOrderId = receipt.PurchaseOrderId;

        if (!postingsAlreadyLanded)
        {
            try
            {
                await inventoryService.ReceiveStockAsync(
                    StockSourceType.GoodsReceipt,
                    receiptId,
                    receipt.LocationId,
                    receipt.Lines
                        .Select(x => new StockInLine(
                            x.ProductId,
                            x.Id,
                            x.Quantity,
                            x.UnitCostBase,
                            x.Id.ToString()))
                        .ToList(),
                    receipt.ReceivedByUserId ?? SystemUser,
                    cancellationToken);
            }
            catch (ValidationException ex)
            {
                // Only void when Inventory confirms nothing landed; otherwise fall through and apply.
                if (!await AnyLandedAsync(
                    inventoryService,
                    StockSourceType.GoodsReceipt,
                    receiptId,
                    clock,
                    cancellationToken))
                {
                    logger.LogWarning(
                        ex,
                        "Inventory refused goods receipt {ReceiptId}; voiding it.",
                        receiptId);

                    receipt.Void(VoidReason, clock.UtcNow);

                    await context.SaveChangesAsync(cancellationToken);

                    return new PostingOutcome(PostingOutcomeKind.Refused, ex);
                }
            }
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (receipt.Status != GoodsReceiptStatus.Posted)
                {
                    var purchaseOrder = await context.PurchaseOrders
                        .Include(x => x.Lines)
                        .FirstAsync(x => x.Id == purchaseOrderId, cancellationToken);

                    receipt.MarkPosted(clock.UtcNow);

                    purchaseOrder.ApplyReceipt(
                        receipt.Lines
                            .Select(x => (x.PurchaseOrderLineId, x.Quantity))
                            .ToList(),
                        clock.UtcNow);
                }

                await context.SaveChangesAsync(cancellationToken);

                return new PostingOutcome(PostingOutcomeKind.Completed);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxCommitAttempts)
            {
                // The stock already landed; only Purchasing's own bookkeeping lost a race. Start over from
                // fresh state, which also resolves to "already posted" if the sweep got there first.
                context.ChangeTracker.Clear();

                var fresh = await context.GoodsReceipts
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == receiptId, cancellationToken);

                if (fresh is null || fresh.Status != GoodsReceiptStatus.Posting)
                    return new PostingOutcome(PostingOutcomeKind.Completed);

                receipt = fresh;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(StockRecordedMessage);
            }
        }
    }

    /// <summary>
    /// Issues the return's stock from the receiving location (all-or-nothing, strict no-oversell), then
    /// records the cost removed per line, the frozen expected credit, and the informational returned
    /// quantities on the purchase order. On a failure Inventory is asked whether the postings landed
    /// anyway: if so the (idempotent) issue is repeated to recover the results and the return is rolled
    /// forward. Only when nothing landed and the failure is a real refusal (see <see cref="IsRefusal"/>)
    /// is the return aborted back to Draft; a transient failure leaves it in Posting for the sweep.
    /// Once the stock has left, commit 2 is retried against freshly loaded state if a concurrent write
    /// rotated a token (the results already recovered are reused); if that keeps failing, a conflict that
    /// says the movement was recorded is thrown and the sweep finishes the return.
    /// </summary>
    public static async Task<PostingOutcome> PostReturnAsync(
        PurchasingDbContext context,
        IInventoryService inventoryService,
        PurchaseReturn purchaseReturn,
        IDateTime clock,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var returnId = purchaseReturn.Id;
        var performedBy = purchaseReturn.PostedBy ?? SystemUser;

        IReadOnlyList<StockPostingResult> results;

        try
        {
            results = await IssueAsync(inventoryService, purchaseReturn, performedBy, cancellationToken);
        }
        catch (Exception ex) when (ex is ConflictException or ValidationException)
        {
            if (await AnyLandedAsync(
                inventoryService,
                StockSourceType.PurchaseReturn,
                returnId,
                clock,
                cancellationToken))
            {
                // Postings landed despite the failure: never abort, roll forward.
                results = await IssueAsync(inventoryService, purchaseReturn, performedBy, cancellationToken);
            }
            else if (IsRefusal(ex))
            {
                purchaseReturn.AbortPost();

                await context.SaveChangesAsync(cancellationToken);

                return new PostingOutcome(PostingOutcomeKind.Refused, ex);
            }
            else
            {
                return new PostingOutcome(PostingOutcomeKind.Pending, ex);
            }
        }

        var costRemoved = results.ToDictionary(x => x.SourceLineId, x => x.ValueBase);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                purchaseReturn.CompletePost(costRemoved, clock.UtcNow);

                var purchaseOrder = await context.PurchaseOrders
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == purchaseReturn.PurchaseOrderId, cancellationToken);

                if (purchaseOrder is null)
                {
                    logger.LogError(
                        "Purchase order {PurchaseOrderId} of purchase return {ReturnId} was not found; the informational returned quantities were not recorded.",
                        purchaseReturn.PurchaseOrderId,
                        returnId);
                }
                else
                {
                    foreach (var line in purchaseReturn.Lines)
                        purchaseOrder.RegisterReturn(line.PurchaseOrderLineId, line.Quantity);
                }

                await context.SaveChangesAsync(cancellationToken);

                return new PostingOutcome(PostingOutcomeKind.Completed);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxCommitAttempts)
            {
                context.ChangeTracker.Clear();

                var fresh = await context.PurchaseReturns
                    .Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.Id == returnId, cancellationToken);

                if (fresh is null || fresh.Status != PurchaseReturnStatus.Posting)
                    return new PostingOutcome(PostingOutcomeKind.Completed);

                purchaseReturn = fresh;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(StockRecordedMessage);
            }
        }
    }

    /// <summary>
    /// Whether a failed issue is a real, deterministic refusal rather than a transient error: a
    /// <see cref="ValidationException"/>, or Inventory's typed <see cref="InsufficientStockException"/>
    /// shortage. Any other <see cref="ConflictException"/> (for example "Stock was modified concurrently")
    /// is transient.
    /// </summary>
    internal static bool IsRefusal(Exception exception) =>
        exception is ValidationException or InsufficientStockException;

    private static Task<IReadOnlyList<StockPostingResult>> IssueAsync(
        IInventoryService inventoryService,
        PurchaseReturn purchaseReturn,
        string performedByUserId,
        CancellationToken cancellationToken) =>
        inventoryService.IssueStockAsync(
            StockSourceType.PurchaseReturn,
            purchaseReturn.Id,
            purchaseReturn.LocationId,
            purchaseReturn.Lines
                .Select(x => new StockOutLine(
                    x.ProductId,
                    x.Id,
                    x.Quantity,
                    x.Id.ToString()))
                .ToList(),
            performedByUserId,
            cancellationToken);

    // postedBefore is "now" (taken after the failure), not a grace-period cutoff: this check only ever
    // rolls forward, so it must see postings written moments ago by the very call that just failed.
    private static async Task<bool> AnyLandedAsync(
        IInventoryService inventoryService,
        StockSourceType sourceType,
        long sourceId,
        IDateTime clock,
        CancellationToken cancellationToken) =>
        (await inventoryService.FilterSourceIdsWithUnreversedPostingsAsync(
            sourceType,
            [sourceId],
            clock.UtcNow,
            cancellationToken)).Count > 0;
}
