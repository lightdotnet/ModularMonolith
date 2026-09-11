using Approval.Tests.TestSupport;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Approval.Api.Application.Approvals.Commands;
using StarterKit.Approval.Api.Application.Approvals.Queries;
using StarterKit.Approval.Api.Controllers;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Shared;
using Xunit;

namespace Approval.Tests.Controllers;

public class UserApprovalControllerTests
{
    private const string CurrentUserId = "current-user";

    private static (UserApprovalController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new UserApprovalController(new FakeCurrentUser { UserId = CurrentUserId })
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery_ScopedToTheCurrentUser()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var request = new MyApprovalRequestSearchRequest { Relation = ApprovalRelation.Requested };
        var expected = new PagedResult<ApprovalRequestDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SearchMyApprovalsQuery>(q => q.UserId == CurrentUserId && q.Request == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SearchAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldDispatchQuery_ScopedToTheCurrentUser()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<ApprovalRequestDto>.Success(new ApprovalRequestDto { Id = "req-1" });
        mediatorMock
            .Setup(m => m.Send(
                It.Is<GetMyApprovalRequestByIdQuery>(q => q.Id == "req-1" && q.UserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync("req-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task DecideAsync_ShouldDispatchCommand_AsTheCurrentUser()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<DecideApprovalStepCommand>(c =>
                    c.ApprovalRequestId == "req-1" && c.DecidedByUserId == CurrentUserId
                    && c.Approved && c.Comment == "ok"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.DecideAsync("req-1", new DecideApprovalRequest(true, "ok"));

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldForceRequesterUserId_ToTheCurrentUser_RegardlessOfWhatTheClientSent()
    {
        // Arrange: a client could try to create a request "as" someone else - the endpoint must
        // never trust the caller-supplied RequesterUserId.
        var (controller, mediatorMock) = CreateSut();
        var spoofedRequest = new CreateApprovalRequest(
            "Leave", "req-1", "someone-else", "someone-else", null, "Title", null, null, null,
            [new ApproverStepInput(1, "approver-1", "approver-1")]);
        var expected = Result<string>.Success("new-id");
        mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateApprovalRequestCommand>(c => c.Model.RequesterUserId == CurrentUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PostAsync(spoofedRequest);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
        mediatorMock.Verify(
            m => m.Send(
                It.Is<CreateApprovalRequestCommand>(c => c.Model.RequesterUserId != "someone-else"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
