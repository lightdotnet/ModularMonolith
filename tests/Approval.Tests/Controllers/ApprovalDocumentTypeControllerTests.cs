using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Approval.Api.Application.DocumentTypes.Commands;
using StarterKit.Approval.Api.Application.DocumentTypes.Queries;
using StarterKit.Approval.Api.Controllers;
using StarterKit.Approval.Contracts.DocumentTypes;
using StarterKit.Shared;
using Xunit;

namespace Approval.Tests.Controllers;

/// <summary>
/// Only the mediator-dispatch wiring is unit-tested here — the class-level
/// `[MustHavePermission(DocumentTypes.View)]` gate itself is ASP.NET Core authorization
/// middleware behavior, not something a plain controller-instantiation test exercises.
/// </summary>
public class ApprovalDocumentTypeControllerTests
{
    private static (ApprovalDocumentTypeController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(mediatorMock.Object).BuildServiceProvider(),
        };
        var controller = new ApprovalDocumentTypeController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task GetListAsync_ShouldDispatchListQuery()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        IList<ApprovalDocumentTypeDto> queryResult = [new ApprovalDocumentTypeDto { Id = "dt-1" }];
        mediatorMock
            .Setup(m => m.Send(
                It.Is<GetApprovalDocumentTypesQuery>(q => q.ActiveOnly == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        // Act
        var response = await controller.GetListAsync(activeOnly: true);

        // Assert: the bare list is wrapped in the standard response envelope by the controller.
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsAssignableFrom<Light.Contracts.IResult<IList<ApprovalDocumentTypeDto>>>(objectResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Same(queryResult, result.Data);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldDispatchGetByIdQuery()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result<ApprovalDocumentTypeDto>.Success(new ApprovalDocumentTypeDto { Id = "dt-1" });
        mediatorMock
            .Setup(m => m.Send(
                It.Is<GetApprovalDocumentTypeByIdQuery>(q => q.Id == "dt-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetByIdAsync("dt-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PostAsync_ShouldDispatchCreateCommand()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var request = new CreateApprovalDocumentTypeRequest("Invoice", "INV", null, true);
        var expected = Result<string>.Success("dt-1");
        mediatorMock
            .Setup(m => m.Send(
                It.Is<CreateApprovalDocumentTypeCommand>(c => c.Model == request),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PostAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnError_WhenRouteIdDoesNotMatchBody()
    {
        // Arrange
        var (controller, _) = CreateSut();

        // Act
        var response = await controller.PutAsync("dt-1", new ApprovalDocumentTypeDto { Id = "dt-2" });

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsAssignableFrom<Light.Contracts.IResult>(objectResult.Value);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task PutAsync_ShouldDispatchUpdateCommand_WhenIdsMatch()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var dto = new ApprovalDocumentTypeDto { Id = "dt-1", Name = "Invoice", Code = "INV", IsActive = true };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<UpdateApprovalDocumentTypeCommand>(c => c.Model == dto),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PutAsync("dt-1", dto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDispatchDeleteCommand()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<DeleteApprovalDocumentTypeCommand>(c => c.Id == "dt-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.DeleteAsync("dt-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
