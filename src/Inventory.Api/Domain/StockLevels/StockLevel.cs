using Light.Exceptions;
using StarterKit.Inventory.Contracts.Exceptions;
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

    /// <summary>
    /// Total value of the on-hand stock in base currency, decimal(19,4). Always equals the sum of the
    /// <c>ValueDeltaBase</c> of the ledger; zero whenever <see cref="QuantityOnHand"/> is zero.
    /// </summary>
    public decimal TotalValueBase { get; private set; }

    /// <summary>Moving-average unit cost — derived, never stored; zero when nothing is on hand.</summary>
    public decimal AverageCostBase => QuantityOnHand == 0
        ? 0m
        : Round(TotalValueBase / QuantityOnHand);

    // Long arithmetic so an extreme delta can neither wrap around nor slip past the guard; the upper
    // bound keeps the running total representable as an int.
    public bool CanApply(int delta) =>
        (long)QuantityOnHand + delta is >= 0 and <= int.MaxValue;

    /// <summary>Value of receiving <paramref name="quantity"/> units at <paramref name="unitCostBase"/>.</summary>
    public static decimal ValueOfInbound(
        int quantity,
        decimal unitCostBase) =>
        Round(quantity * unitCostBase);

    /// <summary>
    /// Value that leaves the level when <paramref name="quantity"/> units are issued at the moving
    /// average. Issuing everything on hand returns the ENTIRE remaining value, so rounding residue can
    /// never linger on an empty level.
    /// </summary>
    public decimal ValueOfOutbound(int quantity)
    {
        if (QuantityOnHand == 0)
            return 0m;

        return quantity >= QuantityOnHand
            ? TotalValueBase
            : Round(TotalValueBase * quantity / QuantityOnHand);
    }

    /// <summary>Unit cost implied by a <paramref name="value"/> spread over <paramref name="quantity"/> units.</summary>
    public static decimal UnitCostOf(
        decimal value,
        int quantity) =>
        quantity == 0 ? 0m : Round(value / quantity);

    /// <summary>Value change needed to make the on-hand quantity worth <paramref name="newUnitCostBase"/> each.</summary>
    public decimal ValueChangeForRevaluation(decimal newUnitCostBase) =>
        Round(QuantityOnHand * newUnitCostBase) - TotalValueBase;

    /// <summary>
    /// Applies a signed quantity change together with its signed value change. Refuses a result below
    /// zero quantity or value, and a result that holds value with no quantity.
    /// </summary>
    public void Apply(
        int quantityDelta,
        decimal valueDelta = 0m)
    {
        if (!CanApply(quantityDelta))
        {
            throw new InsufficientStockException(
                $"Insufficient stock for product {ProductId} at location {LocationId}: available {QuantityOnHand}, requested {-quantityDelta}.");
        }

        var newQuantity = checked(QuantityOnHand + quantityDelta);
        var newValue = TotalValueBase + valueDelta;

        if (newValue < 0m)
            throw Invalid(nameof(valueDelta), "Stock value cannot become negative.");

        if (newQuantity == 0 && newValue != 0m)
            throw Invalid(nameof(valueDelta), "An empty stock level must hold no value.");

        QuantityOnHand = newQuantity;
        TotalValueBase = newValue;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    /// <summary>Rotates the optimistic-concurrency token; called by <c>InventoryDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
