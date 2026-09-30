using Microsoft.Extensions.Logging;
using Moq;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Notifications.Contracts;
using StarterKit.Modules.Notifications.IntegrationEvents;
using Xunit;

namespace Notifications.Tests.IntegrationEvents;

public class UserProvisionedConsumerTests
{
    private static (UserProvisionedConsumer Consumer, Mock<IMailService> MailService, Mock<ILogger<UserProvisionedConsumer>> Logger) CreateSut()
    {
        var mailServiceMock = new Mock<IMailService>();
        var loggerMock = new Mock<ILogger<UserProvisionedConsumer>>();

        return (new UserProvisionedConsumer(mailServiceMock.Object, loggerMock.Object), mailServiceMock, loggerMock);
    }

    private static UserProvisionedIntegrationEvent CreateEvent(
        ProvisioningSource source,
        string email = "jane@example.com",
        string userName = "jane.doe",
        string? firstName = "Jane",
        string? lastName = "Doe") =>
        new(
            "user-1",
            email,
            userName,
            firstName,
            lastName,
            source,
            1);

    [Fact]
    public async Task Handle_ShouldSendExternalLoginWelcome_WhenSourceIsExternalLogin()
    {
        // Arrange
        var (consumer, mailServiceMock, _) = CreateSut();

        // Act
        await consumer.Handle(CreateEvent(ProvisioningSource.ExternalLogin));

        // Assert
        mailServiceMock.Verify(
            m => m.SendFromSystemAsync(
                It.Is<List<string>>(r => r.Count == 1 && r[0] == "jane@example.com"),
                "Welcome to the system",
                It.Is<string>(b => b.Contains("Dear Jane Doe,") && b.Contains("Microsoft")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddressExternalLoginWelcomeToEmail_WhenNameIsMissing()
    {
        // Arrange
        var (consumer, mailServiceMock, _) = CreateSut();

        // Act
        await consumer.Handle(CreateEvent(
            ProvisioningSource.ExternalLogin,
            firstName: null,
            lastName: null));

        // Assert
        mailServiceMock.Verify(
            m => m.SendFromSystemAsync(
                It.IsAny<List<string>>(),
                "Welcome to the system",
                It.Is<string>(b => b.Contains("Dear jane@example.com,")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSendCredentialsWelcome_WhenSourceIsAdmin()
    {
        // Arrange
        var (consumer, mailServiceMock, _) = CreateSut();

        // Act
        await consumer.Handle(CreateEvent(ProvisioningSource.Admin));

        // Assert
        mailServiceMock.Verify(
            m => m.SendFromSystemAsync(
                It.Is<List<string>>(r => r.Count == 1 && r[0] == "jane@example.com"),
                "Welcome to system!",
                It.Is<string>(b => b.Contains("Dear jane.doe,") && b.Contains("<b>Username</b>: jane.doe")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddressAdminWelcomeToEmail_WhenUserNameIsEmpty()
    {
        // Arrange
        var (consumer, mailServiceMock, _) = CreateSut();

        // Act
        await consumer.Handle(CreateEvent(ProvisioningSource.Admin, userName: ""));

        // Assert
        mailServiceMock.Verify(
            m => m.SendFromSystemAsync(
                It.IsAny<List<string>>(),
                "Welcome to system!",
                It.Is<string>(b => b.Contains("Dear jane@example.com,")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotSendMail_WhenEmailIsEmpty()
    {
        // Arrange
        var (consumer, mailServiceMock, _) = CreateSut();

        // Act
        await consumer.Handle(CreateEvent(ProvisioningSource.Admin, email: ""));

        // Assert
        mailServiceMock.Verify(
            m => m.SendFromSystemAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSwallowAndLogError_WhenMailServiceThrows()
    {
        // Arrange
        var (consumer, mailServiceMock, loggerMock) = CreateSut();
        var failure = new InvalidOperationException("SMTP down");
        mailServiceMock
            .Setup(m => m.SendFromSystemAsync(
                It.IsAny<List<string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        // Act
        var exception = await Record.ExceptionAsync(() => consumer.Handle(CreateEvent(ProvisioningSource.Admin)));

        // Assert
        Assert.Null(exception);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
