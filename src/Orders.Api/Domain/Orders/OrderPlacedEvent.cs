using StarterKit.Shared.Entities;

namespace StarterKit.Orders.Api.Domain.Orders;

internal sealed record OrderPlacedEvent(
    long OrderId,
    string LocationId,
    DateTimeOffset PlacedAt) : DomainEvent;
