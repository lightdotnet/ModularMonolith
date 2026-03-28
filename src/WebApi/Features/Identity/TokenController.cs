using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Monolith.Identity;
using Monolith.Identity.Jwt;

namespace Monolith.Features.Identity;

[Route("api/v{version:apiVersion}/oauth")]
public class TokenController(
    ITokenService tokenService,
    JwtTokenMananger jwtTokenMananger) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("token/get")]
    public async Task<IActionResult> GetToken(
        [FromQuery] string? deviceId,
        [FromQuery] string? deviceName,
        [FromBody] GetTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var res = await tokenService.GetTokenAsync(
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

        var res = await tokenService.RefreshTokenAsync(
            request.AccessToken,
            request.RefreshToken,
            new DeviceDto
            {
                IpAddress = ipAddress,
            });

        return Ok(res);
    }

    [HttpGet("token/check")]
    public async Task<IActionResult> CheckToken()
    {
        var accessToken = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        var res = await jwtTokenMananger.IsTokenValidAsync(accessToken);
        return Ok(res);
    }
}