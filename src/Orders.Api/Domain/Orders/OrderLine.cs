using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// A single product line on an <see cref="Orders.Order"/>, own <c>Id</c>, individually removable —
/// same "child entity in a normal <c>HasMany</c> collection, not an owned type" shape as
/// <c>StarterKit.Approval.Api.Domain.Approvals.ApprovalStep</c> on
/// <c>StarterKit.Approval.Api.Domain.Approvals.ApprovalRequest</c>. <see cref="ProductId"/>/
/// <see cref="ProductName"/>/<see cref="Sku"/>/<see cref="UnitPrice"/>/<see cref="VatRate"/> are
/// snapshots taken once from Catalog at <see cref="Create"/> time and never re-read — this line must
/// keep showing what the customer was actually charged even if the product is later repriced or
/// renamed. <see cref="Sku"/> is a plain <c>string</c>, not Catalog's <c>Sku</c> value object: that
/// type lives in Catalog.Api's internals, out of reach across the module boundary, and re-validating
/// an already-Catalog-validated code on every read would add nothing here.
/// </summary>
public class OrderLine : AuditableEntity<long>
{
    private OrderLine()
    {
    }

    private OrderLine(
        long orderId,
        string orderCode,
        long productId,
        string productName,
        string sku,
        Money unitPrice,
        VatPercentage vatRate,
        int quantity,
        Money? requestedSalePrice)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        VatRate = vatRate;
        Quantity = quantity;
        RequestedSalePrice = requestedSalePrice;
    }

    public long OrderId { get; private set; }

    /// <summary>Denormalized snapshot of the parent <c>Order.OrderCode.Value</c> taken at <see cref="Create"/> time — same treatment as <see cref="Sku"/>, no join back to <c>Orders</c> needed to read it.</summary>
    public string OrderCode { get; private set; } = null!;

    public long ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public Money UnitPrice { get; private set; } = null!;

    public VatPercentage VatRate { get; private set; } = null!;

    public int Quantity { get; private set; }

    /// <summary>A manual price override for this line, or <c>null</c> to sell at <see cref="UnitPrice"/>.</summary>
    public Money? RequestedSalePrice { get; private set; }

    public virtual Order Order { get; private set; } = null!;

    public decimal DiscountAmountPerUnit =>
        UnitPrice.Amount - (RequestedSalePrice?.Amount ?? UnitPrice.Amount);

    public decimal DiscountPercentage =>
        UnitPrice.Amount == 0 ? 0 : DiscountAmountPerUnit / UnitPrice.Amount * 100;

    internal static OrderLine Create(
        long orderId,
        string orderCode,
        long productId,
        string productName,
        string sku,
        Money unitPrice,
        VatPercentage vatRate,
        int quantity,
        Money? requestedSalePrice)
    {
        GuardQuantity(quantity);
        GuardSalePriceCap(unitPrice, requestedSalePrice);

        return new OrderLine(
            orderId,
            orderCode,
            productId,
            productName,
            sku,
            unitPrice,
            vatRate,
            quantity,
            requestedSalePrice);
    }

    internal void UpdateQuantity(int quantity)
    {
        GuardQuantity(quantity);

        Quantity = quantity;
    }

    /// <summary>
    /// Replacing a tracked owned <see cref="Money"/> reference with a brand-new instance leaves the
    /// new values unpersisted — see <see cref="Money"/>'s own doc comment — so an already-present
    /// override is mutated in place via <see cref="Money.Update"/> instead of being reassigned. The
    /// very first assignment (from <c>null</c>) and a clear back to <c>null</c> are plain reference
    /// assignments instead (an <c>Added</c>/<c>Deleted</c> owned entity respectively, neither of
    /// which hits that hazard).
    /// </summary>
    internal void SetRequestedSalePrice(Money? salePrice)
    {
        GuardSalePriceCap(UnitPrice, salePrice);

        if (salePrice is null)
        {
            RequestedSalePrice = null;
            return;
        }

        if (RequestedSalePrice is null)
            RequestedSalePrice = salePrice;
        else
            RequestedSalePrice.Update(salePrice.Amount, salePrice.Currency);
    }

    private static void GuardQuantity(int quantity)
    {
        if (quantity <= 0)
            throw Invalid(nameof(quantity), "Quantity must be greater than zero.");
    }

    private static void GuardSalePriceCap(Money unitPrice, Money? salePrice)
    {
        if (salePrice is not null && salePrice.Amount > unitPrice.Amount)
            throw Invalid("requestedSalePrice", "Sale price cannot exceed the catalog price.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
