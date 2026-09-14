using StarterKit.Shared.Entities;

namespace StarterKit.Orders.Api.Domain.Orders;

internal sealed record OrderFulfilledEvent(
    long OrderId,
    string LocationId,
    DateTimeOffset FulfilledAt) : DomainEvent;
