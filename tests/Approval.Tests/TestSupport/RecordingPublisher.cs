using Light.Mediator;

namespace Approval.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IPublisher"/> that records every published notification, so tests can
/// assert on domain / integration events without a real mediator. Shared by
/// <see cref="ApprovalTestHost"/> (dispatches the aggregate's queued domain events on
/// <c>SaveChangesAsync</c>) and <c>ApprovalService</c> (publishes the cross-module integration
/// event) — both resolve the same singleton instance.
/// </summary>
public sealed class RecordingPublisher : IPublisher
{
    public List<INotification> Published { get; } = [];

    public IEnumerable<T> OfType<T>() => Published.OfType<T>();

    public void Clear() => Published.Clear();

    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        Published.Add(notification);
        return Task.CompletedTask;
    }
}
