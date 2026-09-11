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

/// <summary>
/// Only the mediator-dispatch wiring is unit-tested here — the class-level
/// `[MustHavePermission(ViewAll)]` gate itself is ASP.NET Core authorization middleware
/// behavior, not something a plain controller-instantiation test exercises.
/// </summary>
public class ApprovalControllerTests
{
    private static (ApprovalController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new ApprovalController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery_Unrestricted()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var request = new ApprovalRequestSearchRequest { RequestType = "Leave" };
        var expected = new PagedResult<ApprovalRequestDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchApprovalRequestsQuery>(q => q.Request == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SearchAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldDispatchQuery_ForAnyRequestId()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<ApprovalRequestDto>.Success(new ApprovalRequestDto { Id = "req-1" });
        mediatorMock
            .Setup(m => m.Send(It.Is<GetApprovalRequestByIdQuery>(q => q.Id == "req-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync("req-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCommand_WithoutOverridingTheRequester()
    {
        // Arrange: unlike UserApprovalController.PostAsync, the admin harness trusts the caller's
        // chosen requester/approver chain as-is.
        var (controller, mediatorMock) = CreateSut();
        var request = new CreateApprovalRequest(
            "Leave", "req-1", "any-requester", "any-requester", null, "Title", null, null, null,
            [new ApproverStepInput(1, "approver-1", "approver-1")]);
        var expected = Result<string>.Success("new-id");
        mediatorMock
            .Setup(m => m.Send(It.Is<CreateApprovalRequestCommand>(c => c.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PostAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
