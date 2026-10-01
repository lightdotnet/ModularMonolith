using Identity.Tests.TestSupport;
using Moq;
using StarterKit.Modules.Identity.Application.Roles.Commands;
using StarterKit.Modules.Identity.Application.Roles.Services;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Application.Roles.Commands;

public class DeleteRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnGuardError_AndNotCallService_WhenTheRoleGrantsAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(new FakeCurrentUser { UserId = "actor-id" });
        var role = new Role { Name = "Admins" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(
            role,
            new Claim(ClaimTypeConstants.Permission, "roles.manage"))).Succeeded);
        var roleServiceMock = new Mock<IRoleService>();
        var handler = new DeleteRoleCommandHandler(roleServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        roleServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
