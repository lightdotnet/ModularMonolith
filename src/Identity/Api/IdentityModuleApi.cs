using Microsoft.AspNetCore.Identity;
using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Persistence;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared;
using StarterKit.Shared.Constants;
using System.Linq.Expressions;

namespace StarterKit.Modules.Identity.Api;

/// <summary>
/// In-process implementation of the Identity module's cross-module seam.
/// </summary>
internal sealed class IdentityModuleApi(
    IdentityDbContext context,
    UserManager<User> userManager,
    IUserService userService)
    : IIdentityModuleApi
{
    private static readonly Expression<Func<User, UserSummary>> ToSummary = u => new UserSummary(
        u.Id,
        u.UserName ?? string.Empty,
        u.Email ?? string.Empty,
        u.FirstName,
        u.LastName,
        u.Status.Value == ActiveStatus.State.Active);

    public Task<UserSummary?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        return context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(ToSummary)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return [];

        return await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(ToSummary)
            .ToListAsync(ct);
    }

    public async Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = userManager.NormalizeEmail(email);

        return await context.Users
            .AsNoTracking()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(ToSummary)
            .FirstOrDefaultAsync(ct);
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
