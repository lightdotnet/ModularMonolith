using Light.ActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ClaimTypes = Monolith.Claims.ClaimTypes;

namespace Monolith.Identity.Jwt;

internal class TokenService(
    IOptions<JwtOptions> jwtOptions,
    JwtTokenMananger jwtTokenMananger,
    IActiveDirectoryService domainService) : ITokenService
{
    private readonly UserManager<User> _userManager = jwtTokenMananger.UserManager;

    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<IResult<TokenDto>> GetTokenAsync(
        string username, string password,
        DeviceDto? device = null)
    {
        var user = await _userManager.FindByNameAsync(username);

        var errorResult = Result<TokenDto>.Error("Invalid credentials");

        if (user is null || await CheckInvalidUser(user))
            return errorResult;

        bool isPasswordValid;

        if (user.AuthProvider == AuthProvider.AD.ToString())
        {
            isPasswordValid = await domainService.CheckPasswordSignInAsync(username, password);
        }
        else
        {
            var checkLocalPassword = await _userManager.CheckPasswordAsync(user, password);
            isPasswordValid = checkLocalPassword;
        }

        if (isPasswordValid is false)
        {
            return errorResult;
        }

        var tokenExpiresAt = DateTime.Now.AddSeconds(_jwt.AccessTokenExpirationSeconds);
        var refreshTokenExpiresAt = DateTime.Today.AddDays(_jwt.RefreshTokenExpirationDays);

        var token = await jwtTokenMananger.GenerateTokenByAsync(
            user,
            _jwt.Issuer,
            _jwt.SecretKey,
            tokenExpiresAt,
            refreshTokenExpiresAt,
            device);

        return Result<TokenDto>.Success(token);
    }

    public async Task<IResult<TokenDto>> RefreshTokenAsync(
        string accessToken, string refreshToken,
        DeviceDto? device = null)
    {
        // get UserPrincipal from expired token
        var userPrincipal = JwtHelper.GetPrincipalFromExpiredToken(
            accessToken,
            jwtOptions.Value.Issuer,
            jwtOptions.Value.SecretKey,
            ClaimTypes.Role);

        // get userID from UserPrincipal
        var userId = userPrincipal.FindFirstValue(ClaimTypes.UserId);

        if (string.IsNullOrEmpty(userId))
            return Result<TokenDto>.Unauthorized("Error when read info from token.");

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || await CheckInvalidUser(user))
            return Result<TokenDto>.Unauthorized("Invalid credentials.");

        var tokenExpiresAt = DateTime.Now.AddSeconds(_jwt.AccessTokenExpirationSeconds);
        var refreshTokenExpiresAt = DateTime.Today.AddDays(_jwt.RefreshTokenExpirationDays);

        var token = await jwtTokenMananger.RefreshTokenAsync(
            user,
            refreshToken,
            _jwt.Issuer,
            _jwt.SecretKey,
            tokenExpiresAt,
            refreshTokenExpiresAt,
            ClaimTypes.Role, ClaimTypes.UserId,
            device);

        return Result<TokenDto>.Success(token);
    }

    public virtual Task<bool> CheckInvalidUser(User user)
    {
        var isInvalid = user.Status.IsActive is false // user is not active
            || user.Deleted != null; // user is deleted

        return Task.FromResult(isInvalid);
    }
}
