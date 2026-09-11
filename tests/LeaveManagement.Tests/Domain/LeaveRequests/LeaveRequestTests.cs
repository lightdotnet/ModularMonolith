using LeaveManagement.Tests.TestSupport;
using Light.Exceptions;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Domain.LeaveRequests;

public class LeaveRequestTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    private static DateRange Period() => new(Start, End);

    [Fact]
    public void Create_ShouldSucceed_WithValidArguments()
    {
        // Act
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), "Family trip");

        // Assert
        Assert.Equal("user-1", request.UserId);
        Assert.Equal("employee-1", request.EmployeeId);
        Assert.Equal(LeaveType.Annual, request.LeaveType);
        Assert.Equal(Start, request.Period.Start);
        Assert.Equal(End, request.Period.End);
        Assert.Equal("Family trip", request.Reason);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Null(request.ApprovalRequestId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowValidationException_WhenUserIdIsBlank(string userId)
    {
        // Act & Assert
        Assert.Throws<ValidationException>(
            () => LeaveRequest.Create(userId, "employee-1", LeaveType.Annual, Period(), null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowValidationException_WhenEmployeeIdIsBlank(string employeeId)
    {
        // Act & Assert
        Assert.Throws<ValidationException>(
            () => LeaveRequest.Create("user-1", employeeId, LeaveType.Annual, Period(), null));
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenPeriodIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, null!, null));
    }

    [Fact]
    public void LinkApprovalRequest_ShouldSetApprovalRequestId_WhenPendingAndUnlinked()
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act
        request.LinkApprovalRequest("approval-1");

        // Assert
        Assert.Equal("approval-1", request.ApprovalRequestId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LinkApprovalRequest_ShouldThrowValidationException_WhenApprovalRequestIdIsBlank(string approvalRequestId)
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act & Assert
        Assert.Throws<ValidationException>(() => request.LinkApprovalRequest(approvalRequestId));
    }

    [Fact]
    public void LinkApprovalRequest_ShouldThrowConflictException_WhenAlreadyLinked()
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);
        request.LinkApprovalRequest("approval-1");

        // Act & Assert
        Assert.Throws<ConflictException>(() => request.LinkApprovalRequest("approval-2"));
    }

    [Fact]
    public void LinkApprovalRequest_ShouldThrowConflictException_WhenNotPending()
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, LeaveRequestStatus.Approved);

        // Act & Assert
        Assert.Throws<ConflictException>(() => request.LinkApprovalRequest("approval-1"));
    }

    [Theory]
    [InlineData("user-1", true)]
    [InlineData("someone-else", false)]
    public void IsOwnedBy_ShouldReturnExpected(string candidateUserId, bool expected)
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act & Assert
        Assert.Equal(expected, request.IsOwnedBy(candidateUserId));
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Pending, true)]
    [InlineData(LeaveRequestStatus.Rejected, true)]
    [InlineData(LeaveRequestStatus.Approved, false)]
    [InlineData(LeaveRequestStatus.Cancelled, false)]
    public void IsOwnerActionable_ShouldReflectStatus(LeaveRequestStatus status, bool expected)
    {
        // Arrange
        var request = LeaveRequestBuilder.Build("user-1", "employee-1", LeaveType.Annual, Start, End, status);

        // Act & Assert
        Assert.Equal(expected, request.IsOwnerActionable);
    }

    [Fact]
    public void ApplyApprovalOutcome_ShouldReturnTrueAndUpdateStatus_WhenMatchingIdPendingAndNewOutcome()
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, LeaveRequestStatus.Pending,
            approvalRequestId: "approval-1");

        // Act
        var changed = request.ApplyApprovalOutcome("approval-1", LeaveRequestStatus.Approved);

        // Assert
        Assert.True(changed);
        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
    }

    [Fact]
    public void ApplyApprovalOutcome_ShouldReturnFalseAndLeaveUnchanged_WhenApprovalRequestIdMismatch()
    {
        // Arrange — the row has since been resubmitted against a fresh workflow
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, LeaveRequestStatus.Pending,
            approvalRequestId: "current-approval");

        // Act
        var changed = request.ApplyApprovalOutcome("superseded-approval", LeaveRequestStatus.Approved);

        // Assert
        Assert.False(changed);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void ApplyApprovalOutcome_ShouldReturnFalseAndLeaveUnchanged_WhenAlreadyFinalized(
        LeaveRequestStatus currentStatus)
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, currentStatus,
            approvalRequestId: "approval-1");

        // Act
        var changed = request.ApplyApprovalOutcome("approval-1", LeaveRequestStatus.Rejected);

        // Assert
        Assert.False(changed);
        Assert.Equal(currentStatus, request.Status);
    }

    [Fact]
    public void ApplyApprovalOutcome_ShouldReturnFalse_WhenOutcomeEqualsCurrentStatus()
    {
        // Arrange — reconcile call finding nothing changed upstream (still Pending)
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, LeaveRequestStatus.Pending,
            approvalRequestId: "approval-1");

        // Act
        var changed = request.ApplyApprovalOutcome("approval-1", LeaveRequestStatus.Pending);

        // Assert
        Assert.False(changed);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Pending)]
    [InlineData(LeaveRequestStatus.Rejected)]
    public void Resubmit_ShouldApplyFieldsAndResetToPending_WhenOwnerActionable(LeaveRequestStatus status)
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, status,
            reason: "Old reason", approvalRequestId: "old-approval");
        var newPeriod = new DateRange(Start.AddDays(20), End.AddDays(20));

        // Act
        request.Resubmit(LeaveType.Sick, newPeriod, "New reason", "new-approval");

        // Assert
        Assert.Equal(LeaveType.Sick, request.LeaveType);
        Assert.Equal(newPeriod.Start, request.Period.Start);
        Assert.Equal(newPeriod.End, request.Period.End);
        Assert.Equal("New reason", request.Reason);
        Assert.Equal("new-approval", request.ApprovalRequestId);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void Resubmit_ShouldThrowConflictException_WhenNotOwnerActionable(LeaveRequestStatus status)
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, status, approvalRequestId: "old-approval");

        // Act & Assert
        Assert.Throws<ConflictException>(
            () => request.Resubmit(LeaveType.Sick, Period(), "New reason", "new-approval"));
    }

    [Fact]
    public void Resubmit_ShouldThrowArgumentNullException_WhenPeriodIsNull()
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => request.Resubmit(LeaveType.Sick, null!, "New reason", "new-approval"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resubmit_ShouldThrowValidationException_WhenNewApprovalRequestIdIsBlank(string newApprovalRequestId)
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => request.Resubmit(LeaveType.Sick, Period(), "New reason", newApprovalRequestId));
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Pending)]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void ReviseDetails_ShouldApplyFields_RegardlessOfStatus(LeaveRequestStatus status)
    {
        // Arrange
        var request = LeaveRequestBuilder.Build(
            "user-1", "employee-1", LeaveType.Annual, Start, End, status, approvalRequestId: "approval-1");
        var newPeriod = new DateRange(Start.AddDays(20), End.AddDays(20));

        // Act
        request.ReviseDetails(LeaveType.Sick, newPeriod, "Corrected reason");

        // Assert — status and approval linkage are never touched by a manage-only correction
        Assert.Equal(LeaveType.Sick, request.LeaveType);
        Assert.Equal(newPeriod.Start, request.Period.Start);
        Assert.Equal(newPeriod.End, request.Period.End);
        Assert.Equal("Corrected reason", request.Reason);
        Assert.Equal(status, request.Status);
        Assert.Equal("approval-1", request.ApprovalRequestId);
    }

    [Fact]
    public void ReviseDetails_ShouldThrowArgumentNullException_WhenPeriodIsNull()
    {
        // Arrange
        var request = LeaveRequest.Create("user-1", "employee-1", LeaveType.Annual, Period(), null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => request.ReviseDetails(LeaveType.Sick, null!, "Corrected reason"));
    }
}
