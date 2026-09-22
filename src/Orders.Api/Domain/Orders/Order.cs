using Light.Exceptions;
using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// Aggregate root for a single sale, from draft cart through placement, payment reconciliation and
/// fulfillment or cancellation. Every state transition runs through a guarded behaviour method —
/// <see cref="Create"/>, the draft-only line/fee/discount editors, <see cref="Place"/>,
/// <see cref="Cancel"/>, <see cref="MarkFulfilled"/>, <see cref="ReconcilePaymentStatus"/> — rather
/// than open setters. <see cref="Subtotal"/>/<see cref="DiscountAmount"/>/<see cref="FeesTotal"/>/
/// <see cref="Total"/> are computed as plain <c>decimal</c>, not <see cref="Money"/>: a draft order
/// can transiently carry a discount that exceeds its subtotal (that cap is only enforced at
/// <see cref="Place"/> time), and <see cref="Money"/>'s own constructor guard would throw on a
/// negative intermediate value before the dedicated "discount cannot exceed subtotal"/"total cannot
/// be negative" guards below ever got a chance to run.
/// </summary>
public class Order : AuditableEntity<long>
{
    private readonly List<OrderLine> _lines = [];
    private readonly List<OrderFee> _fees = [];

    private Order()
    {
    }

    public string LocationId { get; private set; } = null!;

    /// <summary>
    /// The currency everything on this order is computed and stored in — the base currency at creation
    /// time, immutable afterwards. Every <see cref="Money"/> entering the order (line prices, sale
    /// prices, fees, payments) must be in it; foreign catalog prices are converted before they reach
    /// the aggregate, with the conversion kept as a snapshot on the line.
    /// </summary>
    public string CurrencyCode { get; private set; } = null!;

    /// <summary>Opaque forward-compat slot for a loyalty/member identifier — no behaviour hangs off it yet.</summary>
    public string? MemberId { get; private set; }

    /// <summary>
    /// Unique, human-readable reference — either supplied by the caller at <see cref="Create"/> time
    /// or generated (<see cref="OrderCode.Generate"/>) when omitted. See <see cref="OrderCode"/>'s own
    /// doc comment.
    /// </summary>
    public OrderCode OrderCode { get; private set; } = null!;

    /// <summary>
    /// Opaque reference back to an order in an external system (POS/marketplace/etc.), settable only
    /// at <see cref="Create"/> time. Plain pass-through value, not a VO — no format/uniqueness rule
    /// of its own.
    /// </summary>
    public string? ExternalReferenceCode { get; private set; }

    public OrderStatus Status { get; private set; }

    public OrderDiscount? Discount { get; private set; }

    /// <summary>Cache of payments received against this order, written only by <see cref="ReconcilePaymentStatus"/>.</summary>
    public Money AmountPaid { get; private set; } = null!;

    public DateTimeOffset? PlacedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset? FulfilledAt { get; private set; }

    public string? CancelledReason { get; private set; }

    /// <summary>
    /// Set when the orphaned-stock reconciliation sweep has fenced this never-placed order before
    /// asking Inventory to restore stock. See <see cref="MarkStockReconciled"/>.
    /// </summary>
    public DateTimeOffset? StockReconciledAt { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>OrdersDbContext</c> on every update —
    /// mirrors <c>ApprovalRequest.ConcurrencyToken</c>.
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public IReadOnlyList<OrderFee> Fees => _fees.AsReadOnly();

    public decimal Subtotal =>
        Lines.Sum(x => x.Quantity * (x.RequestedSalePrice?.Amount ?? x.UnitPrice.Amount));

    public decimal DiscountAmount => Discount?.ComputeAmount(Subtotal) ?? 0m;

    public decimal FeesTotal => Fees.Sum(x => x.Amount.Amount);

    public decimal Total => Subtotal - DiscountAmount + FeesTotal;

    /// <summary>
    /// <paramref name="orderCode"/> is the caller-supplied path (pre-checked for uniqueness by the
    /// handler before calling this); omitted (<c>null</c>) is the default system-generated path — see
    /// <see cref="OrderCode"/>'s own doc comment. <paramref name="now"/> is only consumed by the
    /// generated path.
    /// </summary>
    public static Order Create(
        string locationId,
        string? memberId,
        string currencyCode,
        DateTimeOffset now,
        OrderCode? orderCode = null,
        string? externalReferenceCode = null)
    {
        if (string.IsNullOrWhiteSpace(locationId))
            throw Invalid(nameof(locationId), "A location is required.");

        // Money normalizes (trim/upper-case) and shape-checks the code, so the order currency and the
        // AmountPaid currency can never disagree.
        var amountPaid = new Money(0, currencyCode);

        return new Order
        {
            LocationId = locationId,
            MemberId = memberId,
            CurrencyCode = amountPaid.Currency,
            Status = OrderStatus.Draft,
            AmountPaid = amountPaid,
            OrderCode = orderCode ?? OrderCode.Generate(now),
            ExternalReferenceCode = externalReferenceCode,
        };
    }

    public void AddLine(
        long productId,
        string productName,
        string sku,
        int quantity,
        Money unitPrice,
        VatPercentage vatRate,
        Money? requestedSalePrice,
        CatalogPriceSnapshot? catalogPrice = null)
    {
        EnsureDraft();

        ArgumentNullException.ThrowIfNull(unitPrice);
        ArgumentNullException.ThrowIfNull(vatRate);

        EnsureOrderCurrency(unitPrice, nameof(unitPrice));

        if (requestedSalePrice is not null)
            EnsureOrderCurrency(requestedSalePrice, nameof(requestedSalePrice));

        // The snapshot exists exactly when the catalog price had to be converted, i.e. when its
        // currency differs from the order currency; a same-currency line carries none.
        if (catalogPrice is not null)
        {
            if (catalogPrice.CatalogUnitPrice.Currency == CurrencyCode)
                throw Invalid(nameof(catalogPrice), "A catalog price snapshot is only kept when its currency differs from the order currency.");

            if (catalogPrice.AppliedRate <= 0)
                throw Invalid(nameof(catalogPrice), "The applied exchange rate must be greater than zero.");
        }

        var line = OrderLine.Create(
            Id,
            OrderCode.Value,
            productId,
            productName,
            sku,
            unitPrice,
            vatRate,
            quantity,
            requestedSalePrice,
            catalogPrice);

        _lines.Add(line);
    }

    public void UpdateLineQuantity(
        long orderLineId,
        int quantity)
    {
        EnsureDraft();

        FindLine(orderLineId).UpdateQuantity(quantity);
    }

    public void SetLineSalePrice(
        long orderLineId,
        Money? salePrice)
    {
        EnsureDraft();

        if (salePrice is not null)
            EnsureOrderCurrency(salePrice, nameof(salePrice));

        FindLine(orderLineId).SetRequestedSalePrice(salePrice);
    }

    public void RemoveLine(long orderLineId)
    {
        EnsureDraft();

        _lines.Remove(FindLine(orderLineId));
    }

    /// <summary>
    /// Replaces any existing discount wholesale — no "already has one" guard. An already-present
    /// discount is mutated in place via <see cref="OrderDiscount.Update"/> rather than reassigned:
    /// reassigning a tracked owned reference leaves the new values unpersisted (see
    /// <see cref="Money"/>'s own doc comment for the mechanism) — the same reason
    /// <c>StarterKit.Catalog.Api.Domain.Products.Product.Reprice</c> mutates its owned
    /// <see cref="Money"/> in place instead of reassigning it.
    /// </summary>
    public void ApplyDiscount(
        OrderDiscountKind kind,
        decimal value)
    {
        EnsureDraft();

        if (Discount is null)
            Discount = new OrderDiscount(kind, value);
        else
            Discount.Update(kind, value);
    }

    /// <summary>No-op, not an error, if there is no discount to remove.</summary>
    public void RemoveDiscount()
    {
        EnsureDraft();

        Discount = null;
    }

    public void AddFee(
        string name,
        Money amount,
        string feeTypeId,
        string feeTypeName)
    {
        EnsureDraft();

        ArgumentNullException.ThrowIfNull(amount);

        EnsureOrderCurrency(amount, nameof(amount));

        _fees.Add(OrderFee.Create(Id, OrderCode.Value, name, amount, feeTypeId, feeTypeName));
    }

    public void RemoveFee(long orderFeeId)
    {
        EnsureDraft();

        var fee = _fees.FirstOrDefault(x => x.Id == orderFeeId);

        if (fee is null)
            throw Invalid(nameof(orderFeeId), "Order fee not found on this order.");

        _fees.Remove(fee);
    }

    public void Place(DateTimeOffset placedAt)
    {
        if (Status != OrderStatus.Draft)
            throw new ConflictException("Only a draft order can be placed.");

        if (_lines.Count == 0)
            throw Invalid("lines", "At least one order line is required.");

        // Defense in depth: ApplyDiscount/AddFee do not cap against Subtotal as they happen, so a
        // still-editable draft can transiently carry a discount larger than its subtotal — this is
        // the point that invariant is finally enforced.
        if (DiscountAmount > Subtotal)
            throw Invalid("discount", "Discount cannot exceed the order subtotal.");

        if (Total < 0)
            throw Invalid("total", "Order total cannot be negative.");

        Status = OrderStatus.Placed;
        PlacedAt = placedAt;

        AddDomainEvent(new OrderPlacedEvent(Id, LocationId, placedAt));
    }

    public void Cancel(
        string cancelledByUserId,
        string reason,
        DateTimeOffset cancelledAt)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Placed or OrderStatus.PartiallyPaid))
            throw new ConflictException("A paid or fulfilled order cannot be cancelled.");

        if (string.IsNullOrWhiteSpace(reason))
            throw Invalid(nameof(reason), "A cancellation reason is required.");

        Status = OrderStatus.Cancelled;
        CancelledAt = cancelledAt;
        CancelledReason = reason;

        AddDomainEvent(new OrderCancelledEvent(Id, LocationId, cancelledByUserId, cancelledAt, reason));
    }

    public void MarkFulfilled(DateTimeOffset fulfilledAt)
    {
        if (Status != OrderStatus.Paid)
            throw new ConflictException("Only a fully paid order can be marked as fulfilled.");

        Status = OrderStatus.Fulfilled;
        FulfilledAt = fulfilledAt;

        AddDomainEvent(new OrderFulfilledEvent(Id, LocationId, fulfilledAt));
    }

    /// <summary>
    /// Recomputes <see cref="AmountPaid"/> and <see cref="Status"/> from the caller-supplied sum of
    /// non-voided payments against this order. Idempotent, and never downgrades a
    /// <see cref="OrderStatus.Fulfilled"/> order back to a payment-driven status — only the
    /// <see cref="AmountPaid"/> cache is refreshed once fulfilled.
    /// </summary>
    public void ReconcilePaymentStatus(decimal totalPaid)
    {
        if (Status is OrderStatus.Draft or OrderStatus.Cancelled)
            throw new ConflictException("Payments cannot be recorded against a draft or cancelled order.");

        AmountPaid.Update(totalPaid, AmountPaid.Currency);

        if (Status == OrderStatus.Fulfilled)
            return;

        Status = totalPaid switch
        {
            <= 0 => OrderStatus.Placed,
            _ when totalPaid < Total => OrderStatus.PartiallyPaid,
            _ => OrderStatus.Paid,
        };
    }

    /// <summary>
    /// Fence used by the orphaned-stock reconciliation sweep. Only valid for an order that was never
    /// placed. Its sole purpose is to make the entity Modified so <c>OrdersDbContext</c> rotates the
    /// <see cref="ConcurrencyToken"/>: a concurrent <see cref="Place"/> or <see cref="Cancel"/> that
    /// loaded the same token then fails its save with a concurrency conflict, instead of stock being
    /// restored underneath an order that is being placed.
    /// </summary>
    public void MarkStockReconciled(DateTimeOffset at)
    {
        if (PlacedAt is not null)
            throw new ConflictException("Stock of a placed order cannot be reconciled as orphaned.");

        StockReconciledAt = at;
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>OrdersDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Called by <c>CreateOrderCommandHandler</c>'s bounded retry loop after a generated
    /// <see cref="OrderCode"/> collides with another order's — never used for a caller-supplied code
    /// (that path pre-checks and conflicts instead of silently swapping out the caller's choice).
    /// </summary>
    internal void RegenerateOrderCode(DateTimeOffset now) => OrderCode = OrderCode.Generate(now);

    private OrderLine FindLine(long orderLineId)
    {
        var line = _lines.FirstOrDefault(x => x.Id == orderLineId);

        if (line is null)
            throw Invalid(nameof(orderLineId), "Order line not found on this order.");

        return line;
    }

    private void EnsureOrderCurrency(
        Money money,
        string field)
    {
        if (money.Currency != CurrencyCode)
            throw Invalid(field, $"Amount must be in the order currency {CurrencyCode}.");
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new ConflictException("This order is no longer a draft and cannot be modified.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
