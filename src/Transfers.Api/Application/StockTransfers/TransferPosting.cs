using Light.Exceptions;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Inventory.Contracts.Stock;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Transfers.Api.Application.StockTransfers;

internal enum PostingOutcomeKind
{
    /// <summary>Posted to Inventory and committed (dispatch completed / receipt applied).</summary>
    Completed,

    /// <summary>Inventory refused and the refusal was recorded (dispatch aborted to Draft / receipt voided).</summary>
    Refused,

    /// <summary>Failed for a transient reason with nothing landed; the transfer/receipt stays in Posting for a retry.</summary>
    Pending,
}

/// <summary><see cref="Failure"/> is the Inventory exception for <see cref="PostingOutcomeKind.Refused"/>/<see cref="PostingOutcomeKind.Pending"/>.</summary>
internal sealed record PostingOutcome(
    PostingOutcomeKind Kind,
    Exception? Failure = null);

/// <summary>
/// The second half of a dispatch/receive: post to Inventory, then commit the outcome to Transfers.
/// Shared by the command handlers and the reconciliation sweep so both paths behave identically. Every
/// Inventory call here is idempotent per line, so re-running it (a retry, or the sweep finishing a
/// request whose process died) returns the originally posted costs instead of posting twice. Nothing
/// posted to Inventory is ever reversed.
/// </summary>
internal static class TransferPosting
{
    /// <summary>
    /// Issues the transfer's stock from the source location, then completes the dispatch. On a failure
    /// Inventory is asked whether the postings landed anyway: if so the (idempotent) issue is repeated
    /// to recover the costs and the dispatch is rolled forward. Only when nothing landed and the failure
    /// is a real refusal (see <see cref="IsRefusal"/>) is the transfer aborted back to Draft; a transient
    /// failure leaves it in Posting for the sweep to retry.
    /// </summary>
    public static async Task<PostingOutcome> DispatchAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        StockTransfer transfer,
        string performedByUserId,
        IDateTime clock,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<StockPostingResult> results;

        try
        {
            results = await IssueAsync(inventoryService, transfer, performedByUserId, cancellationToken);
        }
        catch (Exception ex) when (ex is ConflictException or ValidationException)
        {
            if (await AnyLandedAsync(
                inventoryService,
                StockSourceType.Transfer,
                transfer.Id,
                clock,
                cancellationToken))
            {
                // Postings landed despite the failure: never abort, roll forward.
                results = await IssueAsync(inventoryService, transfer, performedByUserId, cancellationToken);
            }
            else if (IsRefusal(ex))
            {
                transfer.AbortDispatch();

                await context.SaveChangesAsync(cancellationToken);

                return new PostingOutcome(PostingOutcomeKind.Refused, ex);
            }
            else
            {
                return new PostingOutcome(PostingOutcomeKind.Pending, ex);
            }
        }

        transfer.CompleteDispatch(
            results.ToDictionary(x => x.SourceLineId, x => x.UnitCostBase),
            clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return new PostingOutcome(PostingOutcomeKind.Completed);
    }

    /// <summary>
    /// Receives the receipt's stock at the destination at each line's frozen dispatch cost, then
    /// applies the receipt to the transfer. Positive movements cannot be refused for shortage, so the
    /// only deterministic refusal is a <see cref="ValidationException"/> (destination location gone,
    /// invalid cost): if nothing landed the receipt is voided so it stops blocking the transfer; if
    /// postings landed the receipt is applied without calling Inventory again (costs are already frozen
    /// on the lines). Any other failure leaves the receipt in Posting for a retry — including a
    /// <see cref="ConflictException"/>, which for a receive is only ever the transient
    /// concurrent-modification error.
    /// </summary>
    /// <param name="postingsAlreadyLanded">The caller already knows Inventory holds this receipt's postings, so the call is skipped.</param>
    public static async Task<PostingOutcome> ReceiveAsync(
        TransfersDbContext context,
        IInventoryService inventoryService,
        StockTransfer transfer,
        TransferReceipt receipt,
        string performedByUserId,
        IDateTime clock,
        bool postingsAlreadyLanded,
        CancellationToken cancellationToken)
    {
        if (!postingsAlreadyLanded)
        {
            try
            {
                await inventoryService.ReceiveStockAsync(
                    StockSourceType.TransferReceipt,
                    receipt.Id,
                    transfer.DestinationLocationId,
                    receipt.Lines
                        .Select(receiptLine =>
                        {
                            var line = transfer.Lines.First(x => x.Id == receiptLine.TransferLineId);

                            return new StockInLine(
                                line.ProductId,
                                line.Id,
                                receiptLine.Quantity,
                                line.UnitCostBase,
                                line.Id.ToString());
                        })
                        .ToList(),
                    performedByUserId,
                    cancellationToken);
            }
            catch (ValidationException ex)
            {
                // Only void when Inventory confirms nothing landed; otherwise fall through and apply.
                if (!await AnyLandedAsync(
                    inventoryService,
                    StockSourceType.TransferReceipt,
                    receipt.Id,
                    clock,
                    cancellationToken))
                {
                    transfer.VoidReceipt(
                        receipt.Id,
                        Truncate($"Inventory refused the receipt: {ex.Message}"),
                        clock.UtcNow);

                    await context.SaveChangesAsync(cancellationToken);

                    return new PostingOutcome(PostingOutcomeKind.Refused, ex);
                }
            }
        }

        transfer.CompleteReceive(receipt.Id, clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return new PostingOutcome(PostingOutcomeKind.Completed);
    }

    /// <summary>
    /// Whether a failed issue is a real, deterministic refusal rather than a transient error: a
    /// <see cref="InsufficientStockException"/> (genuine shortage) or a <see cref="ValidationException"/>.
    /// Any other <see cref="ConflictException"/> — e.g. "Stock was modified concurrently", thrown when
    /// Inventory's own optimistic-concurrency retries are exhausted — is transient and is not a refusal.
    /// </summary>
    internal static bool IsRefusal(Exception exception) =>
        exception is InsufficientStockException or ValidationException;

    private static Task<IReadOnlyList<StockPostingResult>> IssueAsync(
        IInventoryService inventoryService,
        StockTransfer transfer,
        string performedByUserId,
        CancellationToken cancellationToken) =>
        inventoryService.IssueStockAsync(
            StockSourceType.Transfer,
            transfer.Id,
            transfer.SourceLocationId,
            transfer.Lines
                .Select(x => new StockOutLine(
                    x.ProductId,
                    x.Id,
                    x.RequestedQuantity,
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

    private static string Truncate(string value) =>
        value.Length <= 1000 ? value : value[..1000];
}
