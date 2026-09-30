using Identity.Tests.TestSupport;
using Microsoft.IdentityModel.Tokens;
using StarterKit.Modules.Identity.Authentication.Jwt;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Authentication.Jwt;

public class JwtSigningServiceTests
{
    private static List<Claim> SampleClaims() =>
    [
        new Claim("uid", "user-1"),
        new Claim("un", "jane.doe"),
    ];

    [Fact]
    public void Generate_ThenValidate_ShouldRoundTripClaims()
    {
        // Arrange
        var service = new JwtSigningService(TestJwtOptions.Create());
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var token = service.Generate(SampleClaims(), expiresAt);
        var principal = service.Validate(token);

        // Assert
        Assert.Equal("user-1", principal.FindFirstValue("uid"));
        Assert.Equal("jane.doe", principal.FindFirstValue("un"));
    }

    [Fact]
    public void Validate_ShouldThrow_WhenIssuerDiffers()
    {
        // Arrange
        var issuerA = new JwtSigningService(TestJwtOptions.Create(issuer: "https://issuer-a"));
        var issuerB = new JwtSigningService(TestJwtOptions.Create(issuer: "https://issuer-b"));
        var token = issuerA.Generate(SampleClaims(), DateTime.UtcNow.AddMinutes(5));

        // Act & Assert
        Assert.ThrowsAny<SecurityTokenException>(() => issuerB.Validate(token));
    }

    [Fact]
    public void Validate_ShouldThrow_WhenSignatureIsTampered()
    {
        // Arrange
        var service = new JwtSigningService(TestJwtOptions.Create());
        var token = service.Generate(SampleClaims(), DateTime.UtcNow.AddMinutes(5));
        var tampered = token[..^4] + (token[^4] == 'A' ? 'B' : 'A') + token[^3..];

        // Act & Assert
        Assert.ThrowsAny<SecurityTokenException>(() => service.Validate(tampered));
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTokenIsExpiredAndExpiredFlagIsFalse()
    {
        // Arrange
        var service = new JwtSigningService(TestJwtOptions.Create());
        var token = service.Generate(SampleClaims(), DateTime.UtcNow.AddMinutes(-5));

        // Act & Assert
        Assert.Throws<SecurityTokenExpiredException>(() => service.Validate(token));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenTokenIsExpiredButExpiredFlagIsTrue()
    {
        // Arrange: this is what the refresh-token flow relies on.
        var service = new JwtSigningService(TestJwtOptions.Create());
        var token = service.Generate(SampleClaims(), DateTime.UtcNow.AddMinutes(-5));

        // Act
        var principal = service.Validate(token, expired: true);

        // Assert
        Assert.Equal("user-1", principal.FindFirstValue("uid"));
    }
}
