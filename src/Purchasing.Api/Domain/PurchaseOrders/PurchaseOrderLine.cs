using StarterKit.Shared.Constants;
using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Domain.PurchaseOrders;

/// <summary>
/// A single product line on a <see cref="PurchaseOrder"/>. <see cref="ProductName"/>/<see cref="Sku"/>
/// are snapshots taken from Catalog when the line is added. The unit cost is a base-currency
/// <see cref="Money"/> exposed as <see cref="UnitCost"/> but persisted as a plain amount column, so a
/// cost edit never has to replace a tracked owned value object. All mutators are internal: only the
/// <see cref="PurchaseOrder"/> aggregate root drives them.
/// </summary>
public class PurchaseOrderLine : AuditableEntity<long>
{
    private PurchaseOrderLine()
    {
    }

    public long PurchaseOrderId { get; private set; }

    public long ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public int OrderedQuantity { get; private set; }

    public int ReceivedQuantity { get; private set; }

    /// <summary>Informational only: quantity sent back to the supplier after receipt. Never reopens the order.</summary>
    public int ReturnedQuantity { get; private set; }

    /// <summary>Base-currency unit cost amount; see <see cref="UnitCost"/>.</summary>
    public decimal UnitCostAmount { get; private set; }

    public virtual PurchaseOrder PurchaseOrder { get; private set; } = null!;

    public Money UnitCost => new(UnitCostAmount, CurrencyConstants.Default);

    /// <summary>
    /// Unit cost times ordered quantity. The request bounds keep this far inside the decimal range; the
    /// saturating fallback only guarantees that reading a total can never throw.
    /// </summary>
    public Money LineTotal => new(SaturatingMultiply(UnitCostAmount, OrderedQuantity), CurrencyConstants.Default);

    public int OutstandingQuantity => OrderedQuantity - ReceivedQuantity;

    internal static PurchaseOrderLine Create(
        long purchaseOrderId,
        long productId,
        string productName,
        string sku,
        int orderedQuantity,
        Money unitCost)
    {
        GuardQuantity(orderedQuantity);

        return new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            OrderedQuantity = orderedQuantity,
            UnitCostAmount = unitCost.Amount,
        };
    }

    internal void Update(
        int orderedQuantity,
        Money unitCost)
    {
        GuardQuantity(orderedQuantity);

        OrderedQuantity = orderedQuantity;
        UnitCostAmount = unitCost.Amount;
    }

    internal void ApplyReceived(int quantity) => ReceivedQuantity += quantity;

    internal void ApplyReturned(int quantity) => ReturnedQuantity += quantity;

    private static decimal SaturatingMultiply(
        decimal amount,
        int quantity)
    {
        try
        {
            return amount * quantity;
        }
        catch (OverflowException)
        {
            return decimal.MaxValue;
        }
    }

    private static void GuardQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(quantity)] = ["Quantity must be greater than zero."],
            });
    }
}
