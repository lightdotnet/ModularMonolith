using LeaveManagement.Tests.TestSupport;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using Xunit;

namespace LeaveManagement.Tests.Application.LeaveRequests.Queries;

public class GetLeaveRequestByIdQueryHandlerTests
{
    private static readonly DateTimeOffset StartDate = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EndDate = new(2026, 4, 5, 0, 0, 0, TimeSpan.Zero);

    private static LeaveRequest MakeEntity(
        string userId,
        LeaveRequestStatus status,
        string? approvalRequestId = null) =>
        LeaveRequestBuilder.Build(
            userId, "employee-1", LeaveType.Annual, StartDate, EndDate, status,
            approvalRequestId: approvalRequestId);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMissing()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery("missing", "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNotOwnerAndCannotManage()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("user-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "user-2", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WhenOwner()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("user-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(entity.Id, result.Data.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WhenCanManageEvenIfNotOwner()
    {
        // Arrange
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("user-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "manager-user", true),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(entity.Id, result.Data.Id);
    }

    [Fact]
    public async Task Handle_ShouldFlattenPeriod_IntoStartDateAndEndDate()
    {
        // Arrange — regression guard for LeaveRequestMappingConfig: without it registered in the
        // test host, Adapt<LeaveRequestDto>() would silently map both dates to default(DateTimeOffset)
        // instead of failing loudly, since Period is a non-nullable complex value object.
        using var host = new LeaveManagementTestHost();
        var entity = MakeEntity("user-1", LeaveRequestStatus.Pending);
        await host.Context.LeaveRequests.AddAsync(entity, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLeaveRequestByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetLeaveRequestByIdQuery(entity.Id, "user-1", false),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StartDate, result.Data.StartDate);
        Assert.Equal(EndDate, result.Data.EndDate);
    }
}
