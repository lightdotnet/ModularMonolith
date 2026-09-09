using Light.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StarterKit.Identity.Api.Controllers;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Controllers;

public class TokenControllerTests
{
    private static (TokenController Controller, Mock<IAuthenticationService> AuthenticationService) CreateSut()
    {
        var authServiceMock = new Mock<IAuthenticationService>();
        var currentUserMock = new Mock<ICurrentUser>();
        var controller = new TokenController(
            authServiceMock.Object,
            currentUserMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        return (controller, authServiceMock);
    }

    [Fact]
    public async Task GetToken_ShouldDelegateToAuthenticationService_WithDeviceInfo()
    {
        // Arrange
        var (controller, authServiceMock) = CreateSut();
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
        var (controller, authServiceMock) = CreateSut();
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
}
