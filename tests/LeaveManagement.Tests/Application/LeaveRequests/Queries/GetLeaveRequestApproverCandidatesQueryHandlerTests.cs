using Moq;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.Organization.Contracts.Services;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Queries;

public class GetLeaveRequestApproverCandidatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnError_WhenNoEmployeeId()
    {
        // Arrange
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        var handler = new GetLeaveRequestApproverCandidatesQueryHandler(orgServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestApproverCandidatesQuery(null),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        orgServiceMock.Verify(
            s => s.GetApproverCandidatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedCandidates_WhenEmployeeLinked()
    {
        // Arrange
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ResolvedApproverDto { EmployeeId = "approver-1", UserId = "user-approver-1", Name = "Alice" },
            ]);
        var handler = new GetLeaveRequestApproverCandidatesQueryHandler(orgServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestApproverCandidatesQuery("employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var candidate = Assert.Single(result.Data);
        Assert.Equal("approver-1", candidate.EmployeeId);
        Assert.Equal("user-approver-1", candidate.UserId);
        Assert.Equal("Alice", candidate.Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithEmptyList_WhenNoCandidatesFound()
    {
        // Arrange
        var orgServiceMock = new Mock<IOrgDirectoryService>();
        orgServiceMock
            .Setup(s => s.GetApproverCandidatesAsync("employee-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new GetLeaveRequestApproverCandidatesQueryHandler(orgServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestApproverCandidatesQuery("employee-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }
}
