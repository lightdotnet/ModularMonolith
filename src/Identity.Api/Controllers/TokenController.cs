using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StarterKit.Identity.Api.ExternalLogin;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Identity.Contracts;
using StarterKit.Infrastructure.Endpoints;
using StarterKit.Shared;

namespace StarterKit.Identity.Api.Controllers;

[ApiExplorerSettings(GroupName = "identity")]
[Route("api/v{version:apiVersion}/auth")]
public class TokenController(
    IAuthenticationService authenticationService,
    IExternalLoginAuthCodeStore externalLoginAuthCodeStore,
    ICurrentUser currentUser) : VersionedApiController
{
    [AllowAnonymous]
    [HttpPost("token/get")]
    public async Task<IActionResult> GetToken(
        [FromQuery] string? deviceId,
        [FromQuery] string? deviceName,
        [FromBody] GetTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var res = await authenticationService.GetTokenAsync(
            request.Username,
            request.Password,
            new DeviceDto
            {
                Id = deviceId,
                Name = deviceName,
                IpAddress = ipAddress,
            });

        return Ok(res);
    }

    [AllowAnonymous]
    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var res = await authenticationService.RefreshTokenAsync(
            request.AccessToken,
            request.RefreshToken,
            new DeviceDto
            {
                IpAddress = ipAddress,
            });

        return Ok(res);
    }

    [AllowAnonymous]
    [EnableRateLimiting("external-login")]
    [HttpPost("token/external")]
    public async Task<IActionResult> ExchangeAuthCode([FromBody] ExchangeAuthCodeRequest request)
    {
        var outcome = await externalLoginAuthCodeStore.ConsumeAsync(request.Code, request.CodeVerifier);

        if (outcome.Status != ExternalLoginAuthCodeStatus.Success)
            return Ok(Result<TokenDto>.Unauthorized("Invalid or expired sign-in code."));

        return Ok(Result<TokenDto>.Success(outcome.Token!));
    }

    [Authorize]
    [HttpPost("token/hub")]
    public async Task<IActionResult> GetHubToken()
    {
        var userId = currentUser.UserId;
        var sessionId = currentUser.SessionId;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            return Ok(Result.Unauthorized());

        var res = await authenticationService.IssueHubTokenAsync(userId, sessionId);

        return Ok(res);
    }
}