using Light.Exceptions;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Inventory.Api.Domain.StockLevels;

/// <summary>
/// Current on-hand quantity of one product at one location — unique per (product, location), created
/// lazily by the first positive movement. It never goes negative: <see cref="Apply"/> is the only
/// mutation and refuses a delta that would take it below zero. The ledger of
/// <c>StockAdjustment</c> rows is the history; this row is the running total of those deltas.
/// </summary>
public class StockLevel : AuditableEntity<long>
{
    private StockLevel()
    {
    }

    public long ProductId { get; private set; }

    public string LocationId { get; private set; } = null!;

    public int QuantityOnHand { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>InventoryDbContext</c> on every update —
    /// mirrors <c>Order.ConcurrencyToken</c>.
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public static StockLevel Create(
        long productId,
        string locationId)
    {
        if (productId <= 0)
            throw Invalid(nameof(productId), "A product is required.");

        if (string.IsNullOrWhiteSpace(locationId))
            throw Invalid(nameof(locationId), "A location is required.");

        return new StockLevel
        {
            ProductId = productId,
            LocationId = locationId,
            QuantityOnHand = 0,
        };
    }

    public bool CanApply(int delta) => QuantityOnHand + delta >= 0;

    public void Apply(int delta)
    {
        if (!CanApply(delta))
        {
            throw new ConflictException(
                $"Insufficient stock for product {ProductId} at location {LocationId}: available {QuantityOnHand}, requested {-delta}.");
        }

        QuantityOnHand += delta;
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>InventoryDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
