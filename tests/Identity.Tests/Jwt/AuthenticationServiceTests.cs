using Identity.Tests.TestSupport;
using Light.ActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Jwt;

public class AuthenticationServiceTests
{
    private static Mock<UserManager<User>> CreateUserManagerMock() =>
        new(Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    private static (AuthenticationService Service, Mock<UserManager<User>> UserManager, Mock<IUserSessionService> SessionService, Mock<IActiveDirectoryService> DomainService, JwtSigningService SigningService)
        CreateSut()
    {
        var userManagerMock = CreateUserManagerMock();
        var sessionServiceMock = new Mock<IUserSessionService>();
        var domainServiceMock = new Mock<IActiveDirectoryService>();
        var options = TestJwtOptions.Create();
        var signingService = new JwtSigningService(options);
        var hubTokenIssuer = new HubTokenIssuer(
            signingService,
            options);
        var dateTime = new FakeDateTime();

        var service = new AuthenticationService(
            options,
            sessionServiceMock.Object,
            userManagerMock.Object,
            domainServiceMock.Object,
            signingService,
            hubTokenIssuer,
            dateTime);

        return (service, userManagerMock, sessionServiceMock, domainServiceMock, signingService);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnError_WhenUsernameIsUnknown()
    {
        // Arrange
        var (service, userManagerMock, _, _, _) = CreateSut();
        userManagerMock.Setup(m => m.FindByNameAsync("nouser")).ReturnsAsync((User?)null);

        // Act
        var result = await service.GetTokenAsync("nouser", "pwd");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnError_WhenUserIsLocked()
    {
        // Arrange
        var (service, userManagerMock, _, _, _) = CreateSut();
        var user = new User { UserName = "locked.user", Status = new ActiveStatus(ActiveStatus.State.Locked) };
        userManagerMock.Setup(m => m.FindByNameAsync("locked.user")).ReturnsAsync(user);

        // Act
        var result = await service.GetTokenAsync("locked.user", "pwd");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnError_WhenUserIsSoftDeleted()
    {
        // Arrange
        var (service, userManagerMock, _, _, _) = CreateSut();
        var user = new User { UserName = "deleted.user", Deleted = DateTimeOffset.UtcNow };
        userManagerMock.Setup(m => m.FindByNameAsync("deleted.user")).ReturnsAsync(user);

        // Act
        var result = await service.GetTokenAsync("deleted.user", "pwd");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnError_WhenLocalPasswordIsWrong()
    {
        // Arrange
        var (service, userManagerMock, _, _, _) = CreateSut();
        var user = new User { UserName = "jane.doe" };
        userManagerMock.Setup(m => m.FindByNameAsync("jane.doe")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        // Act
        var result = await service.GetTokenAsync("jane.doe", "wrong");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldDelegateToSessionService_WhenLocalPasswordIsCorrect()
    {
        // Arrange
        var (service, userManagerMock, sessionServiceMock, _, _) = CreateSut();
        var user = new User { UserName = "jane.doe" };
        var device = new DeviceDto { Id = "device-1" };
        var expectedToken = new TokenDto("access-token", 3600, "refresh-token");
        userManagerMock.Setup(m => m.FindByNameAsync("jane.doe")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        sessionServiceMock
            .Setup(s => s.GenerateTokenAsync(user, It.IsAny<DateTime>(), It.IsAny<DateTime>(), device))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await service.GetTokenAsync("jane.doe", "correct", device);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedToken, result.Data);
        sessionServiceMock.Verify(s => s.GenerateTokenAsync(user, It.IsAny<DateTime>(), It.IsAny<DateTime>(), device), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldUseActiveDirectory_WhenUserAuthProviderIsAD()
    {
        // Arrange
        var (service, userManagerMock, sessionServiceMock, domainServiceMock, _) = CreateSut();
        var user = new User { UserName = "ad.user", AuthProvider = AuthProvider.ActiveDirectory };
        var expectedToken = new TokenDto("access-token", 3600, "refresh-token");
        userManagerMock.Setup(m => m.FindByNameAsync("ad.user")).ReturnsAsync(user);
        domainServiceMock.Setup(d => d.CheckPasswordSignInAsync("ad.user", "pwd")).ReturnsAsync(true);
        sessionServiceMock
            .Setup(s => s.GenerateTokenAsync(user, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await service.GetTokenAsync("ad.user", "pwd");

        // Assert
        Assert.True(result.IsSuccess);
        domainServiceMock.Verify(d => d.CheckPasswordSignInAsync("ad.user", "pwd"), Times.Once);
        userManagerMock.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenAccessTokenHasNoUserIdClaim()
    {
        // Arrange
        var (service, _, _, _, signingService) = CreateSut();
        var accessToken = signingService.Generate([new Claim("un", "jane.doe")], DateTime.UtcNow.AddMinutes(-5));

        // Act
        var result = await service.RefreshTokenAsync(accessToken, "refresh-token");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnUnauthorized_WhenUserIsNotFound()
    {
        // Arrange
        var (service, userManagerMock, _, _, signingService) = CreateSut();
        var accessToken = signingService.Generate([new Claim("uid", "missing-id")], DateTime.UtcNow.AddMinutes(-5));
        userManagerMock.Setup(m => m.FindByIdAsync("missing-id")).ReturnsAsync((User?)null);

        // Act
        var result = await service.RefreshTokenAsync(accessToken, "refresh-token");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldDelegateToSessionService_WhenAccessTokenAndUserAreValid()
    {
        // Arrange
        var (service, userManagerMock, sessionServiceMock, _, signingService) = CreateSut();
        var user = new User { UserName = "jane.doe" };
        var accessToken = signingService.Generate([new Claim("uid", user.Id)], DateTime.UtcNow.AddMinutes(-5));
        var expectedToken = new TokenDto("new-access-token", 3600, "new-refresh-token");
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        sessionServiceMock
            .Setup(s => s.RefreshTokenAsync(user, "refresh-token", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await service.RefreshTokenAsync(accessToken, "refresh-token");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedToken, result.Data);
    }

    [Fact]
    public async Task IssueHubTokenAsync_ShouldReturnHubToken_WhenUserAndSessionAreValid()
    {
        // Arrange
        var (service, userManagerMock, sessionServiceMock, _, _) = CreateSut();
        var user = new User { UserName = "jane.doe" };
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        sessionServiceMock.Setup(s => s.IsTokenValidAsync("session-1")).ReturnsAsync(true);

        // Act
        var result = await service.IssueHubTokenAsync(user.Id, "session-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            TestJwtOptions.Create().Value.HubTokenExpirationSeconds,
            result.Data!.ExpiresIn);
        Assert.False(string.IsNullOrEmpty(result.Data.AccessToken));
    }

    [Fact]
    public async Task IssueHubTokenAsync_ShouldReturnUnauthorized_WhenSessionIsRevoked()
    {
        // Arrange
        var (service, userManagerMock, sessionServiceMock, _, _) = CreateSut();
        var user = new User { UserName = "jane.doe" };
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        sessionServiceMock.Setup(s => s.IsTokenValidAsync("session-1")).ReturnsAsync(false);

        // Act
        var result = await service.IssueHubTokenAsync(user.Id, "session-1");

        // Assert
        Assert.False(result.IsSuccess);
    }
}
