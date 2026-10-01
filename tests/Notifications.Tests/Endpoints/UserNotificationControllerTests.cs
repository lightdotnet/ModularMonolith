using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Endpoints;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using StarterKit.Shared;
using Xunit;

namespace Notifications.Tests.Endpoints;

public class UserNotificationControllerTests
{
    private static (UserNotificationController Controller, Mock<IMediator> Mediator) CreateSut(string userId = "user-1")
    {
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(c => c.UserId).Returns(userId);
        var mediatorMock = new Mock<IMediator>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        var controller = new UserNotificationController(currentUserMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenCurrentUserHasNoUserId()
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(c => c.UserId).Returns((string?)null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UserNotificationController(currentUserMock.Object));
    }

    [Fact]
    public async Task Search_ShouldScopeLookupToTheCurrentUser()
    {
        // Arrange: PagedResult<T> derives from ResultBase, so Ok(...) passes it through unwrapped.
        var (controller, mediatorMock) = CreateSut();
        var lookup = new NotificationLookup { ToUserId = "someone-else" };
        var expected = new PagedResult<NotificationDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchNotificationsQuery>(q => q.Lookup == lookup), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.Search(lookup);

        // Assert
        Assert.Equal("user-1", lookup.ToUserId);
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task Get_ShouldMarkEntryRead_ThenReturnIt()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var dto = new NotificationDto { Id = "entry-1", ToUserId = "user-1", Title = "Hello" };
        var sequence = new MockSequence();
        mediatorMock
            .InSequence(sequence)
            .Setup(m => m.Send(
                It.Is<MarkNotificationReadCommand>(c => c.UserId == "user-1" && c.Id == "entry-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        mediatorMock
            .InSequence(sequence)
            .Setup(m => m.Send(
                It.Is<GetNotificationQuery>(q => q.UserId == "user-1" && q.Id == "entry-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var response = await controller.Get("entry-1");

        // Assert: a plain DTO is not a ResultBase, so Ok(...) wraps it in a Result<T>.
        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<NotificationDto>>(objectResult.Value);
        Assert.Same(dto, wrapped.Data);
        mediatorMock.Verify(
            m => m.Send(It.IsAny<MarkNotificationReadCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CountUnread_ShouldDispatchCountQueryForTheCurrentUser()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<CountUnreadNotificationsQuery>(q => q.UserId == "user-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        // Act
        var response = await controller.CountUnread();

        // Assert: a plain int is not a ResultBase, so Ok(...) wraps it in a Result<int>.
        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<int>>(objectResult.Value);
        Assert.Equal(4, wrapped.Data);
    }
}
