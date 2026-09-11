using Light.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StarterKit.Identity.Api.Controllers;
using StarterKit.Identity.Api.ExternalLogin;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Controllers;

public class TokenControllerTests
{
    private static (
        TokenController Controller,
        Mock<IAuthenticationService> AuthenticationService,
        Mock<IExternalLoginAuthCodeStore> ExternalLoginAuthCodeStore) CreateSut()
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        var externalLoginAuthCodeStoreMock = new Mock<IExternalLoginAuthCodeStore>();
        var currentUserMock = new Mock<ICurrentUser>();
        var controller = new TokenController(
            authServiceMock.Object,
            externalLoginAuthCodeStoreMock.Object,
            currentUserMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return (controller, authServiceMock, externalLoginAuthCodeStoreMock);
    }

    [Fact]
    public async Task GetToken_ShouldDelegateToAuthenticationService_WithDeviceInfo()
    {
        // Arrange
        var (controller, authServiceMock, _) = CreateSut();
        var request = new GetTokenRequest("jane.doe", "pwd");
        var expected = Result<TokenDto>.Success(new TokenDto("access", 3600, "refresh"));
        authServiceMock
            .Setup(s => s.GetTokenAsync(
                "jane.doe",
                "pwd",
                It.Is<DeviceDto>(d => d.Id == "device-1" && d.Name == "Pixel")))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetToken("device-1", "Pixel", request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task RefreshToken_ShouldDelegateToAuthenticationService()
    {
        // Arrange
        var (controller, authServiceMock, _) = CreateSut();
        var request = new RefreshTokenRequest("access-token", "refresh-token");
        var expected = Result<TokenDto>.Success(new TokenDto("new-access", 3600, "new-refresh"));
        authServiceMock
            .Setup(s => s.RefreshTokenAsync("access-token", "refresh-token", It.IsAny<DeviceDto>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.RefreshToken(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task ExchangeAuthCode_ShouldReturnToken_WhenCodeStoreReportsSuccess()
    {
        // Arrange
        var (controller, _, authCodeStoreMock) = CreateSut();
        var request = new ExchangeAuthCodeRequest("valid-code", "verifier");
        var token = new TokenDto("access", 3600, "refresh");
        authCodeStoreMock
            .Setup(s => s.ConsumeAsync("valid-code", "verifier"))
            .ReturnsAsync(new ExternalLoginAuthCodeResult { Status = ExternalLoginAuthCodeStatus.Success, Token = token });

        // Act
        var response = await controller.ExchangeAuthCode(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsType<Result<TokenDto>>(objectResult.Value);
        Assert.True(result.IsSuccess);
        Assert.Same(token, result.Data);
    }

    [Fact]
    public async Task ExchangeAuthCode_ShouldReturnUnauthorized_WhenCodeStoreReportsFailure()
    {
        // Arrange
        var (controller, _, authCodeStoreMock) = CreateSut();
        var request = new ExchangeAuthCodeRequest("expired-code", "verifier");
        authCodeStoreMock
            .Setup(s => s.ConsumeAsync("expired-code", "verifier"))
            .ReturnsAsync(new ExternalLoginAuthCodeResult { Status = ExternalLoginAuthCodeStatus.Failed });

        // Act
        var response = await controller.ExchangeAuthCode(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsType<Result<TokenDto>>(objectResult.Value);
        Assert.False(result.IsSuccess);
    }
}
