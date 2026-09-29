using Light.Domain;
using Light.EventBus.Events;

namespace StarterKit.Shared;

public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        Id = LightId.NewId();
        CreationDate = DateTime.UtcNow;
    }

    public string Id { get; init; }

    public DateTime CreationDate { get; init; }
}
