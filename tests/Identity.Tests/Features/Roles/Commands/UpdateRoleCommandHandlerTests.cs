using Identity.Tests.TestSupport;
using Light.Contracts;
using Moq;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Features.Roles.Commands;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared.Constants;
using Xunit;

namespace Identity.Tests.Features.Roles.Commands;

public class UpdateRoleCommandHandlerTests
{
    private static RoleDto Request(string id, params string[] permissions) => new()
    {
        Id = id,
        Name = "Editors",
        Claims = [.. permissions.Select(p => new ClaimDto { Type = ClaimTypeConstants.Permission, Value = p })],
    };

    [Fact]
    public async Task Handle_ShouldReturnGuardError_AndNotCallService_WhenGrantingAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(new FakeCurrentUser { UserId = "actor-id" });
        var role = new Role { Name = "Editors" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        var roleServiceMock = new Mock<IRoleService>();
        var handler = new UpdateRoleCommandHandler(roleServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(
            new UpdateRoleCommand(Request(role.Id, "roles.manage")),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        roleServiceMock.Verify(s => s.UpdateAsync(It.IsAny<RoleDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallService_WhenGrantingAHeldPermission()
    {
        // Arrange
        var actor = new FakeCurrentUser { UserId = "actor-id" };
        actor.Permissions.Add("users.view");
        using var host = new IdentityTestHost(actor);
        var role = new Role { Name = "Editors" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        var request = Request(role.Id, "users.view");
        var roleServiceMock = new Mock<IRoleService>();
        roleServiceMock.Setup(s => s.UpdateAsync(request)).ReturnsAsync(Result.Success());
        var handler = new UpdateRoleCommandHandler(roleServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new UpdateRoleCommand(request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        roleServiceMock.Verify(s => s.UpdateAsync(request), Times.Once);
    }
}
