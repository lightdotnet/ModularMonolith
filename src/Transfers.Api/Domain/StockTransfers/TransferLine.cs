using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Transfers.Api.Domain.StockTransfers;

/// <summary>
/// A single product line on a <see cref="StockTransfer"/>. <see cref="ProductName"/>/<see cref="Sku"/>
/// are snapshots taken from Catalog when the line is added; <see cref="UnitCostBase"/> is frozen when
/// the transfer is dispatched (the moving-average cost Inventory issued the stock at), so the inbound
/// leg lands at exactly the cost that left the source location. All mutators are internal: only the
/// <see cref="StockTransfer"/> aggregate root drives them.
/// </summary>
public class TransferLine : AuditableEntity<long>
{
    private TransferLine()
    {
    }

    public long TransferId { get; private set; }

    public long ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public int RequestedQuantity { get; private set; }

    public int QtyDispatched { get; private set; }

    public int QtyReceived { get; private set; }

    /// <summary>Quantity written off when the transfer was closed with goods still outstanding.</summary>
    public int QtyClosedShort { get; private set; }

    /// <summary>Base-currency unit cost, set once at dispatch from Inventory's posting result.</summary>
    public decimal UnitCostBase { get; private set; }

    public virtual StockTransfer Transfer { get; private set; } = null!;

    /// <summary>Dispatched, not yet received and not written off.</summary>
    public int QtyInTransit => QtyDispatched - QtyReceived - QtyClosedShort;

    /// <summary>Base-currency value of the quantity written off by a close.</summary>
    public decimal ClosedShortValueBase => QtyClosedShort * UnitCostBase;

    internal static TransferLine Create(
        long transferId,
        long productId,
        string productName,
        string sku,
        int quantity)
    {
        GuardQuantity(quantity);

        return new TransferLine
        {
            TransferId = transferId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            RequestedQuantity = quantity,
        };
    }

    internal void UpdateQuantity(int quantity)
    {
        GuardQuantity(quantity);

        RequestedQuantity = quantity;
    }

    internal void MarkDispatched(decimal unitCostBase)
    {
        QtyDispatched = RequestedQuantity;
        UnitCostBase = unitCostBase;
    }

    internal void ApplyReceived(int quantity) => QtyReceived = checked(QtyReceived + quantity);

    /// <summary>Writes off whatever is still in transit and returns the quantity written off.</summary>
    internal int CloseShort()
    {
        var outstanding = QtyInTransit;

        QtyClosedShort = checked(QtyClosedShort + outstanding);

        return outstanding;
    }

    private static void GuardQuantity(int quantity)
    {
        if (quantity <= 0)
            throw Invalid(nameof(quantity), "Quantity must be greater than zero.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
