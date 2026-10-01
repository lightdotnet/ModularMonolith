using Identity.Tests.TestSupport;
using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Infrastructure.Authentication.Jwt;
using Xunit;

namespace Identity.Tests.Infrastructure.Authentication.Jwt;

public class UserSessionServiceTests
{
    private static UserSessionService CreateService(IdentityTestHost host)
    {
        var signingService = new JwtSigningService(TestJwtOptions.Create());
        var tokenIssuer = new JwtTokenIssuer(host.UserManager, host.Context, signingService);
        return new UserSessionService(tokenIssuer, host.Context, host.DateTime);
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldPersistSession_AndReturnToken()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "session.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = CreateService(host);
        var tokenExpiresAt = host.DateTime.UtcNow.AddHours(1).UtcDateTime;
        var refreshExpiresAt = host.DateTime.UtcNow.AddDays(7).UtcDateTime;
        var device = new DeviceDto { Id = "device-1", Name = "Pixel", IpAddress = "10.0.0.1" };

        // Act
        var token = await service.GenerateTokenAsync(user, tokenExpiresAt, refreshExpiresAt, device);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        Assert.True(token.ExpiresIn > 0);

        var session = await host.Context.UserSessions.AsNoTracking()
            .SingleAsync(s => s.UserId == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal("device-1", session.DeviceId);
        Assert.Equal("Pixel", session.DeviceName);
        Assert.Equal(token.RefreshToken, session.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateRefreshToken_AndUpdateDevice()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "refresh.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = CreateService(host);
        var initial = await service.GenerateTokenAsync(
            user,
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime);
        var newDevice = new DeviceDto { Id = "device-2", Name = "iPhone" };

        // Act
        var refreshed = await service.RefreshTokenAsync(
            user,
            initial.RefreshToken!,
            host.DateTime.UtcNow.AddHours(2).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime,
            newDevice);

        // Assert
        Assert.NotEqual(initial.RefreshToken, refreshed.RefreshToken);
        Assert.NotEqual(initial.AccessToken, refreshed.AccessToken);

        var session = await host.Context.UserSessions.AsNoTracking()
            .SingleAsync(s => s.UserId == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal("device-2", session.DeviceId);
        Assert.Equal(refreshed.RefreshToken, session.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenRefreshTokenIsUnknown()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "unknown.token.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = CreateService(host);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync(
            user,
            "does-not-exist",
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenRefreshTokenIsExpired()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "expired.token.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = CreateService(host);
        var initial = await service.GenerateTokenAsync(
            user,
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddMinutes(-1).UtcDateTime);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync(
            user,
            initial.RefreshToken!,
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenSessionIsRevoked()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "revoked.token.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = CreateService(host);
        var initial = await service.GenerateTokenAsync(
            user,
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime);

        var session = await host.Context.UserSessions.SingleAsync(s => s.UserId == user.Id, TestContext.Current.CancellationToken);
        session.Revoked = true;
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshTokenAsync(
            user,
            initial.RefreshToken!,
            host.DateTime.UtcNow.AddHours(1).UtcDateTime,
            host.DateTime.UtcNow.AddDays(7).UtcDateTime));
    }

    [Fact]
    public async Task GetUserTokensAsync_ShouldExcludeExpiredAndRevokedSessions()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "sessions.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var now = host.DateTime.UtcNow;

        host.Context.UserSessions.AddRange(
            new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(1), RefreshTokenExpiresAt = now.AddDays(1) },
            new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(-1), RefreshTokenExpiresAt = now.AddHours(-1) },
            new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(1), RefreshTokenExpiresAt = now.AddDays(1), Revoked = true });
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(host);

        // Act
        var sessions = await service.GetUserTokensAsync(user.Id);

        // Assert
        Assert.Single(sessions);
    }

    [Fact]
    public async Task IsTokenValidAsync_ShouldReturnTrue_ForActiveSession_AndFalse_ForRevokedExpiredOrUnknown()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "valid.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var now = host.DateTime.UtcNow;

        var active = new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(1) };
        var expired = new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(-1) };
        var revoked = new UserSession { UserId = user.Id, TokenExpiresAt = now.AddHours(1), Revoked = true };
        host.Context.UserSessions.AddRange(active, expired, revoked);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(host);

        // Act & Assert
        Assert.True(await service.IsTokenValidAsync(active.Id));
        Assert.False(await service.IsTokenValidAsync(expired.Id));
        Assert.False(await service.IsTokenValidAsync(revoked.Id));
        Assert.False(await service.IsTokenValidAsync("unknown-id"));
    }

    [Fact]
    public async Task RevokeAsync_ShouldSetRevokedFlag()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "revoke.user" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var session = new UserSession { UserId = user.Id, TokenExpiresAt = host.DateTime.UtcNow.AddHours(1) };
        host.Context.UserSessions.Add(session);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(host);

        // Act
        await service.RevokeAsync(user.Id, session.Id);

        // Assert
        var reloaded = await host.Context.UserSessions.AsNoTracking()
            .SingleAsync(s => s.Id == session.Id, TestContext.Current.CancellationToken);
        Assert.True(reloaded.Revoked);
    }
}
