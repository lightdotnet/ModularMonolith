namespace Monolith.Catalog.Domain.Shops;

internal record ShopStatusUpdatedEvent(string Name, string Status) : DomainEvent;
