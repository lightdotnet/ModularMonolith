namespace StarterKit.Shared.Entities;

public abstract class AuditableEntity<T>
    : Light.Domain.Entities.BaseAuditableEntity<T>
{

}

public abstract class AuditableEntity
    : Light.Domain.Entities.AuditableEntity
{

}