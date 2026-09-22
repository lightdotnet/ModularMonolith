using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace Orders.Tests.TestSupport;

/// <summary>
/// Thin convenience wrappers around <see cref="Order"/>'s own public/internal guarded API — every
/// state reachable through it is built by actually driving the real state machine (<c>Create</c> →
/// <c>AddLine</c> → <c>Place</c> → ...), never by reflection, since <see cref="Order"/>'s surface is
/// small enough that every status query/application tests need is reachable this way.
/// </summary>
internal static class OrderBuilder
{
    public static Order Draft(
        string locationId = "location-1",
        string? memberId = null,
        DateTimeOffset? now = null,
        string? orderCode = null,
        string? externalReferenceCode = null,
        string currencyCode = CurrencyConstants.Default) =>
        Order.Create(
            locationId,
            memberId,
            currencyCode,
            now ?? DateTimeOffset.UtcNow,
            orderCode is null ? null : new OrderCode(orderCode),
            externalReferenceCode);

    public static void AddLine(
        Order order,
        long productId = 1,
        string productName = "Widget",
        string sku = "SKU-1",
        decimal unitPrice = 100m,
        decimal vatRate = 10m,
        int quantity = 1,
        decimal? requestedSalePrice = null,
        string currency = CurrencyConstants.Default,
        CatalogPriceSnapshot? catalogPrice = null) =>
        order.AddLine(
            productId,
            productName,
            sku,
            quantity,
            new Money(unitPrice, currency),
            new VatPercentage(vatRate),
            requestedSalePrice.HasValue ? new Money(requestedSalePrice.Value, currency) : null,
            catalogPrice);

    public static Order DraftWithLine(
        string locationId = "location-1",
        string? memberId = null,
        decimal unitPrice = 100m,
        int quantity = 1)
    {
        var order = Draft(locationId, memberId);

        AddLine(order, unitPrice: unitPrice, quantity: quantity);

        return order;
    }

    public static Order Placed(
        DateTimeOffset placedAt,
        string locationId = "location-1",
        decimal unitPrice = 100m,
        int quantity = 1)
    {
        var order = DraftWithLine(locationId, unitPrice: unitPrice, quantity: quantity);

        order.Place(placedAt);

        return order;
    }

    public static Order Paid(
        DateTimeOffset placedAt,
        string locationId = "location-1",
        decimal unitPrice = 100m,
        int quantity = 1)
    {
        var order = Placed(placedAt, locationId, unitPrice, quantity);

        order.ReconcilePaymentStatus(order.Total);

        return order;
    }
}
