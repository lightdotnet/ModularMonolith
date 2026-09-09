using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using StarterKit.Organization.Contracts.Services;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Commands;

public class CreateLeaveRequestCommandHandlerTests
{
    private static readonly ILogger<CreateLeaveRequestCommandHandler> Logger =
        NullLogger<CreateLeaveRequestCommandHandler>.Instance;

    private static readonly CreateLeaveRequest ValidModel = new(
        LeaveType.Annual,
        new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
        "Family trip",
        "approver-1");

    private static readonly ResolvedApproverDto Approver = new()
    {
        EmployeeId = "approver-1",
        UserId = "approver-user-1",
        Name = "Alice Approver",
    };

    [Fact]
    public async Task Handle_ShouldReject_WhenNotLinkedToEmployee()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", null),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        orgServiceMock.Verify(
            s => s.GetApproverCandidatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenEndDateBeforeStartDate()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);
        var model = ValidModel with { StartDate = ValidModel.EndDate.AddDays(1) };

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(model, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNoApproverCandidates()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenApproverSelectionInvalid()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Approver]);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);
        var model = ValidModel with { ApproverEmployeeId = "someone-else" };

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(model, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenApprovalSucceeds()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Approver]);
        orgServiceMock
            .Setup(s => s.GetEmployeeNameAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Jane Requester");
        CreateApprovalRequest? capturedRequest = null;
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateApprovalRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(Result<string>.Success("approval-1"));
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — approval created first, then a single local commit carrying its id
        Assert.True(result.IsSuccess);
        var entity = Assert.Single(host.Context.LeaveRequests);
        Assert.Equal(result.Data, entity.Id);
        Assert.Equal("approval-1", entity.ApprovalRequestId);
        Assert.Equal(LeaveRequestStatus.Pending, entity.Status);
        Assert.NotNull(capturedRequest);
        Assert.Equal("Jane Requester", capturedRequest!.RequesterName);
        var approverStep = Assert.Single(capturedRequest.ApproverChain);
        Assert.Equal(Approver.EmployeeId, approverStep.ApproverEmployeeId);
        Assert.Equal(Approver.UserId, approverStep.ApproverUserId);
    }

    [Fact]
    public async Task Handle_ShouldNotPersistLeaveRequest_WhenApprovalCreationFails()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Approver]);
        orgServiceMock
            .Setup(s => s.GetEmployeeNameAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Jane Requester");
        var approvalServiceMock = new Mock<IApprovalService>();
        approvalServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Error("boom"));
        var handler = new CreateLeaveRequestCommandHandler(
            host.Context, orgServiceMock.Object, approvalServiceMock.Object, Logger);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — nothing is written locally before the approval workflow exists
        Assert.False(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }
}
