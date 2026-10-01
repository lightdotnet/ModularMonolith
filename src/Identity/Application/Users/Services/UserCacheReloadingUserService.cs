using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Domain.Events;

namespace StarterKit.Modules.Identity.Application.Users.Services;

/// <summary>
/// Decorates <see cref="UserService"/> to publish a <see cref="UserChangedEvent"/>
/// after every successful create, update or delete, so the cached user list is reloaded.
/// </summary>
/// <remarks>
/// A decorator rather than a change to <see cref="UserService"/> itself, so that service stays
/// untouched; every other member passes straight through.
/// </remarks>
internal sealed class UserCacheReloadingUserService(
    UserService inner,
    IPublisher publisher)
    : IUserService
{
    public Task<IEnumerable<UserDto>> GetAllAsync() => inner.GetAllAsync();

    public Task<IResult<UserDto>> GetByIdAsync(string id) => inner.GetByIdAsync(id);

    public Task<IResult<UserDto>> GetByUserNameAsync(string userName) =>
        inner.GetByUserNameAsync(userName);

    public async Task<IResult<string>> CreateAsync(CreateUserRequest newUser)
    {
        var result = await inner.CreateAsync(newUser).ConfigureAwait(false);

        if (result.IsSuccess)
            await PublishAsync().ConfigureAwait(false);

        return result;
    }

    public async Task<IResult> UpdateAsync(UserDto updateUser)
    {
        var result = await inner.UpdateAsync(updateUser).ConfigureAwait(false);

        if (result.IsSuccess)
            await PublishAsync().ConfigureAwait(false);

        return result;
    }

    public async Task<IResult> DeleteAsync(string id)
    {
        var result = await inner.DeleteAsync(id).ConfigureAwait(false);

        if (result.IsSuccess)
            await PublishAsync().ConfigureAwait(false);

        return result;
    }

    public Task<IResult> ForcePasswordAsync(string id, string password) =>
        inner.ForcePasswordAsync(id, password);

    public Task<IEnumerable<UserDto>> GetUsersHasClaimAsync(string claimType, string claimValue) =>
        inner.GetUsersHasClaimAsync(claimType, claimValue);

    public Task<IResult> SetClaimAsync(
        string userId,
        string claimType,
        string? claimValue) =>
        inner.SetClaimAsync(userId, claimType, claimValue);

    private Task PublishAsync() => publisher.Publish(new UserChangedEvent());
}
