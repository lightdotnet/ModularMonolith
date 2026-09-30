# Project Overview: EventBusMassTransitRabbitMQ

## Purpose

`src/EventBusMassTransitRabbitMQ` (assembly/namespace `StarterKit.EventBusMassTransitRabbitMQ`) is the framework's **integration-event bus**. It wires the vendor `Lightsoft.EventBus.MassTransit.RabbitMQ` package into a host from configuration and gives modules the base types they use to consume integration events:

- one registration call that either connects MassTransit to RabbitMQ or, when the bus is disabled, falls back to a bus that logs and drops every event;
- base consumer and consumer-definition types that fix the framework's delivery policy (concurrency, retries, outbox, per-module queues);
- a base type for a module's consumer registration, discovered by assembly scanning.

Integration events are the messages that cross module (or service) boundaries through the broker. They derive from `StarterKit.Shared.IntegrationEvent` (an abstract record implementing the vendor `IIntegrationEvent`, with a generated `Id` and a UTC `CreationDate`). Domain events (`DomainEvent`) are a separate mechanism: they stay in-process and are dispatched through the mediator by `Persistence` on save.

## Public Surface

| Type | Role |
|---|---|
| `DependencyInjection.AddEventBus(IServiceCollection, IConfiguration, params Assembly[])` | Registers `IEventBus`. Reads the `RabbitMQ` configuration section; if it is missing or `Enable` is `false`, registers `NoOpEventBus` as a singleton. Otherwise calls the vendor `AddRabbitMQEventBus`, passing `Host`/`Username`/`Password`, the assemblies to scan for module consumers, and an exclusion of the `IntegrationEvent` base type from automatic topic/exchange creation. |
| `RabbitMQSettings` | Binding target for the `RabbitMQ` section: `Enable`, `Host`, `Username`, `Password`. |
| `NoOpEventBus` | `IEventBus` used when the bus is disabled: logs the event name and `Id` at Information level and returns without publishing. |
| `AppConsumer<TMessage>` | Abstract consumer base over the vendor `Light.MassTransit.RabbitMQ.Consumer<TMessage>`, constrained to `IntegrationEvent`. A consumer implements `Handle(TMessage)`; the vendor base logs the outcome and, by default, rethrows failures so the endpoint's retry/error policy applies. |
| `AppConsumerDefinition<TEvent, TConsumer>` | Abstract definition over the vendor `ConsumerDefinition<TEvent, TConsumer>`, constrained to `IntegrationEvent`/`AppConsumer<TEvent>`. Sets the delivery policy described under [Design Notes](#design-notes); an optional constructor argument sets a per-module queue-name prefix. |
| `AppModuleConsumer` | Abstract base over the vendor `Light.AspNetCore.Modularity.ModuleConsumer` (shipped in the event-bus package). A module overrides `AddConsumers(IBusRegistrationConfigurator)` to register its consumer/definition pairs. |

`IEventBus` (publishing) and `IIntegrationEvent` are vendor abstractions from `Lightsoft.EventBus`, reached through `Shared`.

## Configuration

```json
"RabbitMQ": {
  "Enable": true,
  "Host": "localhost",
  "Username": "guest",
  "Password": "guest"
}
```

- Section absent, or `Enable: false` → `NoOpEventBus`; no broker connection is attempted and published events are dropped (logged only).
- `Enable: true` → `Host`, `Username` and `Password` are required; the vendor registration throws an `ArgumentException` at startup if any is empty.

## Usage

Define the event (typically in the publishing module's `<Module>.Contracts`), a consumer, its definition, and the module's consumer registration in the consuming module:

```csharp
[BindingName("order-placed")]
public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId)
    : IntegrationEvent;

public sealed class OrderPlacedConsumer(
    ILogger<OrderPlacedConsumer> logger)
    : AppConsumer<OrderPlacedIntegrationEvent>(logger)
{
    public override Task Handle(OrderPlacedIntegrationEvent message)
    {
        // react to the event
        return Task.CompletedTask;
    }
}

public sealed class OrderPlacedConsumerDefinition
    : AppConsumerDefinition<OrderPlacedIntegrationEvent, OrderPlacedConsumer>
{
    public OrderPlacedConsumerDefinition()
        : base("billing")
    {
    }
}

public sealed class BillingModuleConsumer
    : AppModuleConsumer
{
    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<OrderPlacedConsumer, OrderPlacedConsumerDefinition>();
    }
}
```

Register the bus once in the host, passing every assembly that contains an `AppModuleConsumer`:

```csharp
builder.Services.AddEventBus(
    builder.Configuration,
    typeof(BillingModuleConsumer).Assembly);
```

Publish by injecting `IEventBus` and calling `Publish(new OrderPlacedIntegrationEvent(orderId), cancellationToken)`.

## Design Notes

- **Delivery policy** (`AppConsumerDefinition`): at most 10 messages consumed concurrently per consumer; in-memory retries at 200 ms, 1 s, 5 s and 15 s, after which the message moves to the `_error` queue; an in-memory outbox holds messages the consumer publishes until it completes, so a failed or retried attempt publishes nothing.
- **Failure logging**: the vendor consumer base logs an error on every failed attempt, so a message that exhausts its retries produces one error entry per attempt (five in total).
- **Per-module queues**: without a prefix, every consumer of an event with a `[BindingName]` binds to one queue named after the binding name, so consumers compete (load-balancing). Passing a module prefix (e.g. `base("billing")`) yields `billing-<binding-name>`, giving each module its own copy of the event. Per the vendor base, the prefix applies only when the event type itself carries `[BindingName]` (the attribute is not inherited); prefixes may contain only letters, digits, `-`, `_`, `.`, `:`.
- **Consumer discovery**: `AddEventBus` hands its assemblies to the vendor registration, which scans them for `ModuleConsumer` types, instantiates each (parameterless constructor) and calls `AddConsumers`.
- **Disabled bus**: `NoOpEventBus` keeps publishers working with no broker (development, tests); events are not queued for later delivery.

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Shared` | project | `IntegrationEvent` base type (constraint on the consumer bases, excluded from topology); vendor `Lightsoft.EventBus` abstractions (`IEventBus`, `IIntegrationEvent`) flow through it |
| `Lightsoft.EventBus.MassTransit.RabbitMQ` | package | `AddRabbitMQEventBus`, MassTransit/RabbitMQ transport, `Consumer<T>`/`ConsumerDefinition<TEvent, TConsumer>` bases, `ModuleConsumer` and its scanning |

Package versions: `Directory.Packages.props`.

## Depended On By

No project in `StarterKit.slnx` references it, including `tests/Framework.Tests`. It is consumed by host applications and business modules outside this solution. Its only solution reference is `Shared`, which keeps the framework's dependency direction intact.

## Notable Conventions

- The registration entry point is `static class DependencyInjection` (`AddEventBus`), the name most framework feature folders use for their registration class.
- `IEventBus` lifetime depends on configuration: singleton when disabled (`NoOpEventBus`), scoped when enabled (the vendor RabbitMQ bus).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
