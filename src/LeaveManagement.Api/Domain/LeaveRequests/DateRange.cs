using Light.Domain.ValueObjects;
using Light.Exceptions;

namespace StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

/// <summary>
/// The start/end instants a leave request covers, treated as one value. Construction is guarded:
/// an end before the start is rejected with a <see cref="ValidationException"/> — the same
/// validation-exception type the rest of the backend throws for a domain guard failure — so the
/// caller can surface a clean error at the boundary. Mapped as an EF owned type (same table, via
/// <c>OwnsOne</c>) over the existing <c>StartDate</c>/<c>EndDate</c> columns, so it adds no schema —
/// a <c>ComplexProperty</c> mapping was tried first but relational comparisons (<c><=</c>, <c>>=</c>,
/// etc.) on a complex-type member do not translate to SQL in this EF Core version, which the
/// overlap-check query in <c>OverlappingLeaveRequestsSpec</c> needs.
/// <para>
/// Inherits <see cref="ValueObject"/> (the same base <see cref="StarterKit.Shared.ActiveStatus"/>
/// uses for its own <c>OwnsOne</c> mapping) rather than being a plain record: reassigning an owned
/// navigation to a new instance — which <c>Resubmit</c>/<c>ReviseDetails</c> do — makes EF's change
/// tracker emit the old instance as <c>Deleted</c> and the new one as <c>Added</c>;
/// <c>TrackingExtensions.AuditEntries</c> specifically resets any tracked <see cref="ValueObject"/>
/// entry that lands in <c>Deleted</c> back to <c>Unchanged</c> to guard against exactly that. A plain
/// record isn't covered by that guard. Being a class (not a record) also means there is no
/// compiler-generated <c>with</c> expression that could bypass the range guard from outside the type.
/// </para>
/// </summary>
public sealed class DateRange : ValueObject
{
    // EF materialises the owned type through this parameterless constructor + the property
    // setters; stored rows are always already valid, so the guard is not re-run on read.
    private DateRange()
    {
    }

    public DateRange(
        DateTimeOffset start,
        DateTimeOffset end)
    {
        if (end < start)
            throw Invalid(nameof(end), "End date cannot be before start date.");

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; private set; }

    public DateTimeOffset End { get; private set; }

    /// <summary>
    /// Mutates this same tracked instance in place rather than being replaced by a new one —
    /// mirrors <see cref="StarterKit.Shared.ActiveStatus.Update"/>. <see cref="LeaveRequest"/>'s
    /// <c>Period</c> is mapped as an EF owned type (table-split, same row): reassigning the
    /// navigation to a *new* <see cref="DateRange"/> instance makes EF's change tracker emit the
    /// old instance as <c>Deleted</c> and the new one as <c>Added</c>, which
    /// <c>TrackingExtensions.AuditEntries</c> resets to <c>Unchanged</c> (a guard against nulling
    /// the owned columns on replacement) — but that reset then leaves the *new* Start/End values
    /// unpersisted, silently keeping the row's old dates. Updating the existing instance's fields
    /// avoids the replace-the-reference dance entirely.
    /// </summary>
    internal void Update(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw Invalid(nameof(end), "End date cannot be before start date.");

        Start = start;
        End = end;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
