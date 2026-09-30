using MassTransit;
using Microsoft.Extensions.Logging;
using StarterKit.EventBusMassTransitRabbitMQ;

namespace Framework.Tests.EventBusMassTransitRabbitMQ.TestSupport;

public class TestBoundEventConsumer(ILogger<TestBoundEventConsumer> logger)
    : AppConsumer<TestBoundEvent>(logger)
{
    public override Task Handle(TestBoundEvent message) => Task.CompletedTask;
}

public class TestUnboundEventConsumer(ILogger<TestUnboundEventConsumer> logger)
    : AppConsumer<TestUnboundEvent>(logger)
{
    public override Task Handle(TestUnboundEvent message) => Task.CompletedTask;
}

/// <summary>Definition without an endpoint-name prefix.</summary>
public class TestBoundEventConsumerDefinition
    : AppConsumerDefinition<TestBoundEvent, TestBoundEventConsumer>
{
}

/// <summary>Definition with a per-module endpoint-name prefix.</summary>
public class PrefixedTestBoundEventConsumerDefinition()
    : AppConsumerDefinition<TestBoundEvent, TestBoundEventConsumer>("billing")
{
}

/// <summary>Prefixed definition for an event without a binding name.</summary>
public class PrefixedTestUnboundEventConsumerDefinition()
    : AppConsumerDefinition<TestUnboundEvent, TestUnboundEventConsumer>("billing")
{
}

/// <summary>Module consumer picked up by assembly scanning in AddEventBus.</summary>
public class TestModuleConsumer
    : AppModuleConsumer
{
    public override void AddConsumers(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<TestBoundEventConsumer, TestBoundEventConsumerDefinition>();
    }
}
