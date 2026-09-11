using System.Security.Claims;
using LeaveManagement.Tests.TestSupport;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests.Queries;
using StarterKit.LeaveManagement.Api.Controllers;
using StarterKit.LeaveManagement.Contracts.Authorization;
using StarterKit.LeaveManagement.Contracts.LeaveRequests;
using StarterKit.Shared;
using StarterKit.Shared.Constants;
using Xunit;

namespace LeaveManagement.Tests.Controllers;

public class LeaveRequestControllerTests
{
    private const string CurrentUserId = "current-user";
    private const string CurrentEmployeeId = "employee-1";

    private static readonly CreateLeaveRequest CreateModel = new(
        LeaveType.Annual,
        new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
        "Family trip",
        "approver-1");

    private static readonly UpdateLeaveRequest UpdateModel = new(
        LeaveType.Sick,
        new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 2, 3, 0, 0, 0, TimeSpan.Zero),
        "Flu",
        "approver-1");

    private static (LeaveRequestController Controller, Mock<IMediator> Mediator) CreateSut(bool canManage = false)
    {
        var mediatorMock = new Mock<IMediator>();
        var currentUser = new FakeCurrentUser { UserId = CurrentUserId };

        if (canManage)
            currentUser.Permissions.Add(LeaveManagementPermissions.Requests.Manage);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypeConstants.EmployeeId, CurrentEmployeeId)], "TestAuth")),
        };
        var controller = new LeaveRequestController(currentUser)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery_WithCanManageTrue_WhenCallerHoldsManagePermission()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut(canManage: true);
        var request = new LeaveRequestSearchRequest { LeaveType = LeaveType.Annual };
        var expected = new PagedResult<LeaveRequestDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SearchLeaveRequestsQuery>(q =>
                    q.Request == request && q.CurrentUserId == CurrentUserId && q.CanManage),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SearchAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery_WithCanManageFalse_WhenCallerLacksManagePermission()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut(canManage: false);
        var request = new LeaveRequestSearchRequest { LeaveType = LeaveType.Annual };
        var expected = new PagedResult<LeaveRequestDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SearchLeaveRequestsQuery>(q =>
                    q.Request == request && q.CurrentUserId == CurrentUserId && !q.CanManage),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SearchAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldDispatchQuery_WithRouteIdCurrentUserAndCanManage()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut(canManage: true);
        var expected = Result<LeaveRequestDto>.Success(new LeaveRequestDto { Id = "req-1" });
        mediatorMock
            .Setup(m => m.Send(
                It.Is<GetLeaveRequestByIdQuery>(q =>
                    q.Id == "req-1" && q.CurrentUserId == CurrentUserId && q.CanManage),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync("req-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetApproversAsync_ShouldDispatchQuery_WithEmployeeIdFromClaims()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<List<ApproverCandidateDto>>.Success([]);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<GetLeaveRequestApproverCandidatesQuery>(q => q.CurrentEmployeeId == CurrentEmployeeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetApproversAsync();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCommand_WithCurrentUserAndEmployeeIdFromClaims()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<string>.Success("new-id");
        mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateLeaveRequestCommand>(c =>
                    c.Model == CreateModel
                    && c.RequesterUserId == CurrentUserId
                    && c.RequesterEmployeeId == CurrentEmployeeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PostAsync(CreateModel);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PutAsync_ShouldDispatchCommand_WithRouteIdCurrentUserAndCanManage()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut(canManage: true);
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateLeaveRequestCommand>(c =>
                    c.Id == "req-1"
                    && c.Model == UpdateModel
                    && c.CurrentUserId == CurrentUserId
                    && c.CanManage),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PutAsync("req-1", UpdateModel);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDispatchCommand_WithRouteIdCurrentUserAndCanManage()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut(canManage: true);
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<DeleteLeaveRequestCommand>(c =>
                    c.Id == "req-1" && c.CurrentUserId == CurrentUserId && c.CanManage),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.DeleteAsync("req-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
