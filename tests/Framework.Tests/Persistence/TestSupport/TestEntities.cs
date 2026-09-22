using Light.Domain.Entities;
using Light.Domain.Entities.Interfaces;
using Light.Domain.ValueObjects;
using StarterKit.Shared.Entities;

namespace Framework.Tests.Persistence.TestSupport;

/// <summary>
/// Minimal aggregate used to exercise <see cref="StarterKit.Database.TrackingExtensions"/> and
/// <see cref="StarterKit.Database.DispatchDomainEventsExtensions"/> against a real EF Core change tracker.
/// </summary>
public sealed class TestAggregate : BaseAuditableEntity<int>, ISoftDelete
{
    public DateTimeOffset? Deleted { get; set; }

    public string? DeletedBy { get; set; }

    public TestNote? Note { get; set; }

    public void RaiseTestEvent() => AddDomainEvent(new TestDomainEvent());
}

/// <summary>
/// Owned value object mapped on <see cref="TestAggregate.Note"/> so tests can exercise the
/// "deleted ValueObject entries reset to Unchanged" behavior in <see cref="StarterKit.Database.TrackingExtensions"/>.
/// </summary>
public sealed class TestNote : ValueObject
{
    public string Text { get; set; } = string.Empty;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Text;
    }
}

public sealed record TestDomainEvent : DomainEvent;

/// <summary>
/// Standalone entity implementing only <see cref="IHasCreationTime"/> (no <see cref="IHasAuditUser"/> or
/// <see cref="IHasModificationTime"/>), to exercise the partial-audit-capability path in
/// <see cref="StarterKit.Database.TrackingExtensions"/> that the split shared-kernel interfaces enable.
/// </summary>
public sealed class TestCreationOnlyEntity : IHasCreationTime
{
    public int Id { get; set; }

    public DateTimeOffset Created { get; set; }
}
