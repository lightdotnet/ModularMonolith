using Light.Contracts;
using LeaveManagement.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
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

    private static CreateLeaveRequestCommandHandler MakeHandler(
        LeaveManagementTestHost host,
        Mock<IOrgDirectoryService> orgServiceMock,
        Mock<IApprovalService> approvalServiceMock) =>
        new(
            host.Context,
            orgServiceMock.Object,
            new LeaveRequestApprovalCoordinator(host.Context, approvalServiceMock.Object, orgServiceMock.Object),
            approvalServiceMock.Object,
            Logger);

    [Fact]
    public async Task Handle_ShouldReject_WhenNotLinkedToEmployee()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
        var model = ValidModel with { StartDate = ValidModel.EndDate.AddDays(1) };

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(model, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — DateRange's own guard throws, translated to Result.Error by Guard
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
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);
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
    public async Task Handle_ShouldReject_WhenOverlappingPendingRequestExists()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var existing = LeaveRequestBuilder.Build(
            "user-2", "employee-1", LeaveType.Annual, ValidModel.StartDate, ValidModel.EndDate,
            LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(existing, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([Approver]);
        var approvalServiceMock = new Mock<IApprovalService>();
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CreateAsync(It.IsAny<CreateApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(host.Context.LeaveRequests);
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
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

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
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — nothing is written locally before the approval workflow exists
        Assert.False(result.IsSuccess);
        Assert.Empty(host.Context.LeaveRequests);
    }

    [Fact]
    public async Task Handle_ShouldCompensateByCancellingApproval_WhenFinalSaveFails()
    {
        // Arrange — force the final SaveChangesAsync (after LinkApprovalRequest has already run
        // in-memory) to throw a plain exception by disposing the shared Sqlite connection from
        // inside the CreateAsync callback, i.e. exactly between the new approval being created and
        // the local commit. Mirrors UpdateLeaveRequestCommandHandlerTests' equivalent compensation test.
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
            .Callback(() => host.Context.Database.GetDbConnection().Dispose())
            .ReturnsAsync(Result<string>.Success("approval-1"));
        approvalServiceMock
            .Setup(s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — the plain-exception branch (Result.Error), not the ExceptionBase translation.
        // (Not asserting host.Context.LeaveRequests here: disposing the shared :memory: Sqlite
        // connection above tears down its schema entirely, so any further query against this host
        // would itself throw "no such table" — the CancelAsync verification below is sufficient.)
        Assert.False(result.IsSuccess);
        approvalServiceMock.Verify(
            s => s.CancelAsync("approval-1", "user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCompensateAndTranslateException_WhenApprovalServiceReturnsBlankApprovalId()
    {
        // Arrange — LinkApprovalRequest's own guard rejects a blank approval id; a real
        // IApprovalService should never return one, but the compensation boundary still has to
        // translate the ExceptionBase this defense-in-depth guard throws instead of leaking it.
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
            .ReturnsAsync(Result<string>.Success(" "));
        approvalServiceMock
            .Setup(s => s.CancelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var handler = MakeHandler(host, orgServiceMock, approvalServiceMock);

        // Act
        var result = await handler.Handle(
            new CreateLeaveRequestCommand(ValidModel, "user-1", "employee-1"),
            TestContext.Current.CancellationToken);

        // Assert — LinkApprovalRequest's ValidationException, translated by ToResult
        Assert.False(result.IsSuccess);
        Assert.Equal("approvalRequestId: An approval request id is required.", result.Message);
        Assert.Empty(host.Context.LeaveRequests);
        approvalServiceMock.Verify(s => s.CancelAsync(" ", "user-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
