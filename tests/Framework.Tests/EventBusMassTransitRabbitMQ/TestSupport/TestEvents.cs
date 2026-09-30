using Light.EventBus.Events;
using StarterKit.Shared;

namespace Framework.Tests.EventBusMassTransitRabbitMQ.TestSupport;

/// <summary>Integration event with a binding name, so consumer definitions derive the queue name from it.</summary>
[BindingName("test-bound-event")]
public record TestBoundEvent : IntegrationEvent;

/// <summary>Integration event without a binding name.</summary>
public record TestUnboundEvent : IntegrationEvent;
