using Microsoft.AspNetCore.Identity;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Users.Services;
using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Infrastructure.Persistence;
using StarterKit.Shared;
using StarterKit.Shared.Constants;

namespace StarterKit.Modules.Identity.Api;

/// <summary>
/// In-process implementation of the Identity module's cross-module seam.
/// </summary>
internal sealed class IdentityModuleApi(
    IdentityDbContext context,
    UserManager<User> userManager,
    IUserService userService,
    IUserQueryService userQuery)
    : IIdentityModuleApi
{
    public Task<UserSummary?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        return userQuery.GetSummaryAsync(userId, ct);
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return [];

        return await userQuery.GetSummariesAsync(userIds, ct);
    }

    public Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        return userQuery.FindSummaryByEmailAsync(email, ct);
    }

    public async Task<IReadOnlyList<string>> GetUserIdsWithPermissionAsync(
        string permission,
        CancellationToken ct = default)
    {
        return await context
            .QueryUserIdsWithClaim(ClaimTypeConstants.Permission, permission)
            .ToListAsync(ct);
    }

    public async Task<Result<string>> EnsureUserAsync(
        string email,
        string? firstName,
        string? lastName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var existing = await userManager.FindByEmailAsync(email);

        if (existing is not null)
            return Result<string>.Success(existing.Id);

        // Same path as an administrator creating a user, minus the password; it raises the
        // UserProvisionedIntegrationEvent published after the commit.
        var created = await userService.CreateAsync(new CreateUserRequest
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
        });

        return created.IsSuccess
            ? Result<string>.Success(created.Data)
            : Result<string>.Error(created.Message);
    }
}
