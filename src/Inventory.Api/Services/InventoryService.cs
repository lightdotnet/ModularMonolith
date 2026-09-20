using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Locations.Contracts.Services;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Inventory.Api.Services;

internal class InventoryService(
    StockLedger ledger,
    ILocationDirectoryService locationDirectoryService) : IInventoryService
{
    public async Task DecrementForOrderAsync(
        long orderId,
        string locationId,
        IReadOnlyList<StockLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return;

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
                SourceOrderId: orderId,
                SourceOrderLineId: x.OrderLineId,
                IdempotencyKey: $"order-place:{orderId}:{x.OrderLineId}"))
            .ToList();

        await ledger.ApplyAsync(movements, cancellationToken);
    }

    public Task RestoreForOrderAsync(
        long orderId,
        string performedByUserId,
        CancellationToken cancellationToken = default) =>
        ledger.ReverseOrderPlacementsAsync(orderId, performedByUserId, cancellationToken);

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
