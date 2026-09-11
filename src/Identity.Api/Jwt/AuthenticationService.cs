using Light.ActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Identity.Api.Jwt;

internal class AuthenticationService(
    IOptions<JwtOptions> jwtOptions,
    IUserSessionService userSessionService,
    UserManager<User> userManager,
    IActiveDirectoryService domainService,
    JwtSigningService jwtSigningService,
    HubTokenIssuer hubTokenIssuer,
    IDateTime dateTime) : IAuthenticationService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<IResult<TokenDto>> GetTokenAsync(
        string username, string password,
        DeviceDto? device = null)
    {
        var user = await userManager.FindByNameAsync(username);

        var errorResult = Result<TokenDto>.Error("Invalid credentials");

        if (user is null || await CheckInvalidUser(user))
            return errorResult;

        bool isPasswordValid;

        if (user.AuthProvider == AuthProvider.ActiveDirectory)
        {
            isPasswordValid = await domainService.CheckPasswordSignInAsync(username, password);
        }
        else
        {
            var checkLocalPassword = await userManager.CheckPasswordAsync(user, password);
            isPasswordValid = checkLocalPassword;
        }

        if (isPasswordValid is false)
        {
            return errorResult;
        }

        var token = await IssueTokenAsync(user, device);

        return Result<TokenDto>.Success(token);
    }

    public async Task<IResult<TokenDto>> IssueTokenForUserAsync(
        string userId,
        DeviceDto? device = null)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user is null || await CheckInvalidUser(user))
            return Result<TokenDto>.Unauthorized("Invalid credentials.");

        var token = await IssueTokenAsync(user, device);

        return Result<TokenDto>.Success(token);
    }

    public async Task<IResult<TokenDto>> RefreshTokenAsync(
        string accessToken, string refreshToken,
        DeviceDto? device = null)
    {
        // get UserPrincipal from expired token
        var userPrincipal = jwtSigningService.Validate(accessToken, expired: true);

        // get userID from UserPrincipal
        var userId = userPrincipal.FindFirstValue(ClaimTypeConstants.UserId);

        if (string.IsNullOrEmpty(userId))
            return Result<TokenDto>.Unauthorized("Error when read info from token.");

        var user = await userManager.FindByIdAsync(userId);

        if (user is null || await CheckInvalidUser(user))
            return Result<TokenDto>.Unauthorized("Invalid credentials.");

        var (tokenExpiresAt, refreshTokenExpiresAt) = ComputeExpirations();

        var token = await userSessionService.RefreshTokenAsync(
            user,
            refreshToken,
            tokenExpiresAt,
            refreshTokenExpiresAt,
            device);

        return Result<TokenDto>.Success(token);
    }

    public async Task<IResult<HubTokenResponse>> IssueHubTokenAsync(
        string userId,
        string sessionId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user is null || await CheckInvalidUser(user))
            return Result<HubTokenResponse>.Unauthorized("Invalid credentials.");

        // A user who revoked this session must not be able to mint fresh hub tokens
        // against it, even within the access token's remaining lifetime.
        if (await userSessionService.IsTokenValidAsync(sessionId) is false)
            return Result<HubTokenResponse>.Unauthorized("Session is no longer valid.");

        var token = hubTokenIssuer.Issue(user, sessionId);

        return Result<HubTokenResponse>.Success(
            new HubTokenResponse(token, _jwt.HubTokenExpirationSeconds));
    }

    /// <summary>
    /// The shared token-issuance tail behind <see cref="GetTokenAsync"/> and
    /// <see cref="IssueTokenForUserAsync"/>: resolve expirations and hand off to
    /// <see cref="IUserSessionService.GenerateTokenAsync"/>.
    /// </summary>
    private async Task<TokenDto> IssueTokenAsync(User user, DeviceDto? device)
    {
        var (tokenExpiresAt, refreshTokenExpiresAt) = ComputeExpirations();

        return await userSessionService.GenerateTokenAsync(
            user,
            tokenExpiresAt,
            refreshTokenExpiresAt,
            device);
    }

    private (DateTime TokenExpiresAt, DateTime RefreshTokenExpiresAt) ComputeExpirations()
    {
        var now = dateTime.UtcNow.UtcDateTime;

        return (
            now.AddSeconds(_jwt.AccessTokenExpirationSeconds),
            now.AddDays(_jwt.RefreshTokenExpirationDays));
    }

    public virtual Task<bool> CheckInvalidUser(User user)
    {
        var isInvalid = user.Status.IsActive is false // user is not active
            || user.Deleted != null; // user is deleted

        return Task.FromResult(isInvalid);
    }
}
