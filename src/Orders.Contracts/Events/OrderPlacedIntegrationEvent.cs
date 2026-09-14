namespace StarterKit.Orders.Contracts.Events;

/// <summary>
/// In-process cross-module notification raised by Orders immediately after an order is placed and
/// the change is committed — same best-effort-immediate delivery contract as Approval's
/// <c>ApprovalFinalizedIntegrationEvent</c>. A consuming module (e.g. Inventory) subscribes with an
/// <c>INotificationHandler&lt;T&gt;</c>; delivery failures are logged by the publisher and never
/// fail the placing request.
/// </summary>
public sealed record OrderPlacedIntegrationEvent(
    long OrderId,
    string LocationId,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderLineSnapshot> Lines) : INotification;

public sealed record OrderLineSnapshot(
    long ProductId,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal? RequestedSalePrice);
