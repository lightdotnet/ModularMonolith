using Light.Domain.Entities.Interfaces;
using Light.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Monolith.Database;

public static class DispatchDomainEventsExtensions
{
    public static async Task DispatchDomainEvents(this IPublisher mediator, DbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<IEvent>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .OfType<INotification>()
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent);
    }
}
