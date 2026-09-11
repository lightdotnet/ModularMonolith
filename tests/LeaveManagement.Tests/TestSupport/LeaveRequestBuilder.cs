using System.Reflection;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;

namespace LeaveManagement.Tests.TestSupport;

/// <summary>
/// Builds <see cref="LeaveRequest"/> aggregates directly in an arbitrary state for read-model
/// seeding and handler arrange blocks.
/// <para>
/// The domain type encapsulates all state behind private setters and factory/behaviour methods
/// (<see cref="LeaveRequest.Create"/>, <see cref="LeaveRequest.LinkApprovalRequest"/>,
/// <see cref="LeaveRequest.Resubmit"/>, <see cref="LeaveRequest.ApplyApprovalOutcome"/>) that only
/// ever reach specific, guarded transitions. Tests that need a pre-existing row in an arbitrary
/// state (e.g. <c>Approved</c> with a linked approval id) write the private members directly here,
/// mirroring <c>Approval.Tests.TestSupport.ApprovalEntityBuilder</c>.
/// </para>
/// </summary>
internal static class LeaveRequestBuilder
{
    public static LeaveRequest Build(
        string userId,
        string employeeId,
        LeaveType leaveType,
        DateTimeOffset start,
        DateTimeOffset end,
        LeaveRequestStatus status = LeaveRequestStatus.Pending,
        string? reason = null,
        string? approvalRequestId = null)
    {
        var request = New<LeaveRequest>();

        Set(request, nameof(LeaveRequest.UserId), userId);
        Set(request, nameof(LeaveRequest.EmployeeId), employeeId);
        Set(request, nameof(LeaveRequest.LeaveType), leaveType);
        Set(request, nameof(LeaveRequest.Period), new DateRange(start, end));
        Set(request, nameof(LeaveRequest.Reason), reason);
        Set(request, nameof(LeaveRequest.Status), status);
        Set(request, nameof(LeaveRequest.ApprovalRequestId), approvalRequestId);

        return request;
    }

    private static T New<T>() =>
        (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    private static void Set(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }
}
