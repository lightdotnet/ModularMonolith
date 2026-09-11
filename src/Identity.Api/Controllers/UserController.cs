using Light.ActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StarterKit.Identity.Api.Application.Users.Commands;
using StarterKit.Identity.Api.Application.Users.Queries;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;

namespace StarterKit.Identity.Api.Controllers;

[ApiExplorerSettings(GroupName = "identity")]
[MustHavePermission(IdentityPermissions.Users.View)]
public class UserController(
    IUserService userService,
    IActiveDirectoryService activeDirectoryService) : VersionedApiController
{
    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] SearchUserRequest request)
    {
        return Ok(await Mediator.Send(new SearchUserQuery(request)));
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        return Ok(await userService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id)
    {
        return Ok(await userService.GetByIdAsync(id));
    }

    [HttpGet("by_username/{username}")]
    public async Task<IActionResult> GetByUsernameAsync(string username)
    {
        return Ok(await userService.GetByUserNameAsync(username));
    }

    [HttpPost]
    [MustHavePermission(IdentityPermissions.Users.Create)]
    public async Task<IActionResult> PostAsync(
        [FromBody] CreateUserRequest request)
    {
        return Ok(await Mediator.Send(new CreateUserCommand(request)));
    }

    [HttpPut("{id}")]
    [MustHavePermission(IdentityPermissions.Users.Update)]
    public async Task<IActionResult> PutAsync(
        string id,
        [FromBody] UserDto request)
    {
        if (id != request.Id)
        {
            return Ok(Result.Error("Validate User ID not match"));
        }

        return Ok(await Mediator.Send(new UpdateUserCommand(request)));
    }

    [HttpDelete("{id}")]
    [MustHavePermission(IdentityPermissions.Users.Delete)]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        return Ok(await Mediator.Send(new DeleteUserCommand(id)));
    }

    [HttpPut("{id}/password/force")]
    [MustHavePermission(IdentityPermissions.Users.Update)]
    public async Task<IActionResult> ForcePasswordAsync(
        [FromRoute] string id,
        [FromBody] string password)
    {
        return Ok(await Mediator.Send(new ForcePasswordCommand(id, password)));
    }

    [HttpGet("get_domain_user/{userName}")]
    public async Task<IActionResult> GetDomainUserAsync([FromRoute] string userName)
    {
        return Ok(await activeDirectoryService.GetByUserNameAsync(userName));
    }

    [HttpPut("sync_domain_users")]
    public async Task<IActionResult> SyncDomainUsersAsync()
    {
        var users = await userService.GetAllAsync();

        var domainUsers = users.Where(x => x.AuthProvider == AuthProviderWire.ActiveDirectory);

        foreach (var user in domainUsers)
        {
            var domainUser = await activeDirectoryService.GetByUserNameAsync(user.UserName);
            if (domainUser is null)
            {
                user.Status = ActiveStatus.State.Locked.ToString();

                await Mediator.Send(new UpdateUserCommand(user));
            }
        }

        return Ok();
    }
}
