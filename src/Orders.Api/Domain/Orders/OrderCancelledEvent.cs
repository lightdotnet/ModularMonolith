using StarterKit.Shared.Entities;

namespace StarterKit.Orders.Api.Domain.Orders;

internal sealed record OrderCancelledEvent(
    long OrderId,
    string LocationId,
    string CancelledByUserId,
    DateTimeOffset CancelledAt,
    string Reason) : DomainEvent;
