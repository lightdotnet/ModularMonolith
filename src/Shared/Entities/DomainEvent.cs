namespace StarterKit.Shared.Entities;

public abstract record DomainEvent : Light.Domain.Entities.BaseEvent, Light.Mediator.INotification;
