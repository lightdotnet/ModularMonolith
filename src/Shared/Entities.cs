using Light.Domain;

namespace Monolith;

public abstract class AuditableEntity<T> : Light.Domain.Entities.AuditableEntity<T>;

public abstract class AuditableEntity : AuditableEntity<string>
{
    protected AuditableEntity() => Id = LightId.NewId();
}

public record DomainEvent : Light.Domain.Entities.DomainEvent, Light.Mediator.INotification;
