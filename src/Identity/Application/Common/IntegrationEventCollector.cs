using Light.EventBus.Abstractions;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Application.Common;

/// <summary>
/// Scoped buffer of integration events raised while changing Identity state.
/// <see cref="Infrastructure.Persistence.IdentityDbContext"/> publishes them only after the next save commits,
/// and drops them when that save fails.
/// </summary>
/// <remarks>
/// Callers add an event <em>before</em> the save that commits the change it describes, and
/// <see cref="Clear"/> the buffer when that change is rejected without a save (for example a
/// failed <c>IdentityResult</c>), so a stale event never rides along with a later save.
/// Module-local for now; a candidate to promote to Persistence once a second module needs it.
/// </remarks>
internal sealed class IntegrationEventCollector
{
    private readonly List<Func<IEventBus, CancellationToken, Task>> _publishers = [];

    public bool HasEvents => _publishers.Count != 0;

    /// <summary>
    /// Buffers <paramref name="integrationEvent"/>. The publish call is captured with the
    /// compile-time <typeparamref name="TEvent"/> so the bus routes it by its concrete type.
    /// </summary>
    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _publishers.Add((bus, ct) => bus.Publish(integrationEvent, ct));
    }

    /// <summary>
    /// Publishes every buffered event in the order it was added. Does not clear the buffer.
    /// </summary>
    public async Task PublishAllAsync(IEventBus bus, CancellationToken ct)
    {
        foreach (var publish in _publishers.ToArray())
        {
            await publish(bus, ct);
        }
    }

    public void Clear() => _publishers.Clear();
}
