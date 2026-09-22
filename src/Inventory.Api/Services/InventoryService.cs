using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Locations.Contracts.Services;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Inventory.Api.Services;

internal class InventoryService(
    StockLedger ledger,
    ILocationDirectoryService locationDirectoryService) : IInventoryService
{
    public async Task<IReadOnlyList<StockPostingResult>> DecrementForOrderAsync(
        long orderId,
        string locationId,
        IReadOnlyList<StockLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default)
    {
        ValidatePerformer(performedByUserId);

        if (lines.Count == 0)
            return [];

        if (lines.Any(x => x.Quantity <= 0))
            throw Invalid("lines", "Every stock line must have a positive quantity.");

        if (!await locationDirectoryService.ExistsAsync(locationId, cancellationToken))
            throw Invalid(nameof(locationId), $"Location {locationId} not found.");

        var movements = lines
            .Select(x => new StockMovement(
                x.ProductId,
                locationId,
                -x.Quantity,
                StockAdjustmentReason.OrderPlacement,
                performedByUserId,
                SourceType: StockSourceType.Order,
                SourceId: orderId,
                SourceLineId: x.OrderLineId,
                IdempotencyKey: $"order-place:{orderId}:{x.OrderLineId}"))
            .ToList();

        await ledger.ApplyAsync(movements, cancellationToken);

        return await ToResultsAsync(
            movements.Select(x => (x.ProductId, x.SourceLineId!.Value, x.IdempotencyKey!)).ToList(),
            cancellationToken);
    }

    public Task RestoreForOrderAsync(
        long orderId,
        string performedByUserId,
        CancellationToken cancellationToken = default,
        DateTimeOffset? postedBefore = null) =>
        ledger.ReverseOrderPlacementsAsync(
            orderId,
            performedByUserId,
            cancellationToken,
            postedBefore);

    public Task<IReadOnlyList<long>> FilterSourceIdsWithUnreversedPostingsAsync(
        StockSourceType sourceType,
        IReadOnlyCollection<long> candidateSourceIds,
        DateTimeOffset postedBefore,
        CancellationToken cancellationToken = default) =>
        ledger.FilterSourceIdsWithUnreversedPostingsAsync(
            sourceType,
            candidateSourceIds,
            postedBefore,
            cancellationToken);

    public async Task<IReadOnlyList<StockPostingResult>> ReceiveStockAsync(
        StockSourceType sourceType,
        long sourceId,
        string locationId,
        IReadOnlyList<StockInLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default)
    {
        var (reason, keyPrefix) = sourceType switch
        {
            StockSourceType.GoodsReceipt => (StockAdjustmentReason.PurchaseReceipt, "goods-receipt"),
            StockSourceType.TransferReceipt => (StockAdjustmentReason.TransferIn, "transfer-receipt"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceType),
                sourceType,
                "Only GoodsReceipt and TransferReceipt sources can receive stock."),
        };

        ValidatePerformer(performedByUserId);
        ValidateSourceId(sourceId);

        if (lines.Count == 0)
            return [];

        if (lines.Any(x => x.Quantity <= 0))
            throw Invalid("lines", "Every stock line must have a positive quantity.");

        if (lines.Any(x => x.UnitCostBase < 0m))
            throw Invalid("lines", "A unit cost cannot be negative.");

        ValidateLineRefs(
            lines.Select(x => x.IdempotencyRef).ToList(),
            keyPrefix,
            sourceId);

        await EnsureLocationAsync(locationId, cancellationToken);

        var movements = lines
            .Select(x => new StockMovement(
                x.ProductId,
                locationId,
                x.Quantity,
                reason,
                performedByUserId,
                SourceType: sourceType,
                SourceId: sourceId,
                SourceLineId: x.SourceLineId,
                IdempotencyKey: $"{keyPrefix}:{sourceId}:{x.IdempotencyRef}",
                UnitCostBase: x.UnitCostBase))
            .ToList();

        await ledger.ApplyAsync(movements, cancellationToken);

        return await ToResultsAsync(
            movements.Select(x => (x.ProductId, x.SourceLineId!.Value, x.IdempotencyKey!)).ToList(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockPostingResult>> IssueStockAsync(
        StockSourceType sourceType,
        long sourceId,
        string locationId,
        IReadOnlyList<StockOutLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default)
    {
        var (reason, keyPrefix) = sourceType switch
        {
            StockSourceType.Transfer => (StockAdjustmentReason.TransferOut, "transfer-issue"),
            StockSourceType.PurchaseReturn => (StockAdjustmentReason.PurchaseReturnOut, "purchase-return"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceType),
                sourceType,
                "Only Transfer and PurchaseReturn sources can issue stock."),
        };

        ValidatePerformer(performedByUserId);
        ValidateSourceId(sourceId);

        if (lines.Count == 0)
            return [];

        if (lines.Any(x => x.Quantity <= 0))
            throw Invalid("lines", "Every stock line must have a positive quantity.");

        ValidateLineRefs(
            lines.Select(x => x.IdempotencyRef).ToList(),
            keyPrefix,
            sourceId);

        await EnsureLocationAsync(locationId, cancellationToken);

        // The ledger checks every product's net shortage before applying anything, so an issue is
        // all-or-nothing and reports every shortfall in one ConflictException.
        var movements = lines
            .Select(x => new StockMovement(
                x.ProductId,
                locationId,
                -x.Quantity,
                reason,
                performedByUserId,
                SourceType: sourceType,
                SourceId: sourceId,
                SourceLineId: x.SourceLineId,
                IdempotencyKey: $"{keyPrefix}:{sourceId}:{x.IdempotencyRef}"))
            .ToList();

        await ledger.ApplyAsync(movements, cancellationToken);

        return await ToResultsAsync(
            movements.Select(x => (x.ProductId, x.SourceLineId!.Value, x.IdempotencyKey!)).ToList(),
            cancellationToken);
    }

    /// <summary>Reads the posted cost back from the stored adjustments, so a replay returns the same results.</summary>
    private async Task<IReadOnlyList<StockPostingResult>> ToResultsAsync(
        IReadOnlyList<(long ProductId, long SourceLineId, string IdempotencyKey)> lines,
        CancellationToken cancellationToken)
    {
        var posted = await ledger.GetPostedAsync(
            lines.Select(x => x.IdempotencyKey).ToList(),
            cancellationToken);

        return lines
            .Where(x => posted.ContainsKey(x.IdempotencyKey))
            .Select(x =>
            {
                var cost = posted[x.IdempotencyKey];

                return new StockPostingResult(
                    x.ProductId,
                    x.SourceLineId,
                    cost.Quantity,
                    cost.UnitCostBase,
                    cost.ValueBase);
            })
            .ToList();
    }

    private async Task EnsureLocationAsync(
        string locationId,
        CancellationToken cancellationToken)
    {
        if (!await locationDirectoryService.ExistsAsync(locationId, cancellationToken))
            throw Invalid(nameof(locationId), $"Location {locationId} not found.");
    }

    private static void ValidatePerformer(string performedByUserId)
    {
        if (string.IsNullOrWhiteSpace(performedByUserId))
            throw Invalid(nameof(performedByUserId), "A performing user is required.");
    }

    private static void ValidateSourceId(long sourceId)
    {
        if (sourceId <= 0)
            throw Invalid(nameof(sourceId), "A source document is required.");
    }

    /// <summary>Column limit of <c>StockAdjustment.IdempotencyKey</c> (see <c>InventoryDbContext</c>).</summary>
    private const int MaxIdempotencyKeyLength = 200;

    private static void ValidateLineRefs(
        IReadOnlyList<string> refs,
        string keyPrefix,
        long sourceId)
    {
        if (refs.Any(string.IsNullOrWhiteSpace))
            throw Invalid("lines", "Every stock line needs an idempotency reference.");

        // The built key is "{prefix}:{sourceId}:{ref}" and must fit the column.
        var maxRefLength = MaxIdempotencyKeyLength - keyPrefix.Length - sourceId.ToString().Length - 2;

        if (refs.Any(x => x.Length > maxRefLength))
            throw Invalid("lines", $"An idempotency reference cannot exceed {maxRefLength} characters.");

        if (refs.Distinct(StringComparer.Ordinal).Count() != refs.Count)
            throw Invalid("lines", "Idempotency references must be unique within a document.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
