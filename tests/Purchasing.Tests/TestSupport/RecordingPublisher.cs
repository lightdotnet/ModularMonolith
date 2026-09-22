using Light.Mediator;

namespace Purchasing.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IPublisher"/> that records every published notification, so tests can
/// assert on domain / integration events without a real mediator. Mirrors
/// <c>Orders.Tests.TestSupport.RecordingPublisher</c>.
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
