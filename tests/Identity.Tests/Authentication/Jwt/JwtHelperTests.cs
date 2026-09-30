using Identity.Tests.TestSupport;
using StarterKit.Modules.Identity.Authentication.Jwt;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Authentication.Jwt;

public class JwtHelperTests
{
    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyValue()
    {
        // Act
        var token = JwtHelper.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnDifferentValues_AcrossCalls()
    {
        // Act
        var first = JwtHelper.GenerateRefreshToken();
        var second = JwtHelper.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ReadClaims_ShouldRoundTripSingleValueClaims()
    {
        // Arrange
        var signingService = new JwtSigningService(TestJwtOptions.Create());
        var token = signingService.Generate(
            [new Claim(ClaimTypeConstants.UserId, "user-1"), new Claim(ClaimTypeConstants.UserName, "jane.doe")],
            DateTime.UtcNow.AddMinutes(5));

        // Act
        var claims = JwtHelper.ReadClaims(token);

        // Assert
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.UserId && c.Value == "user-1");
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.UserName && c.Value == "jane.doe");
    }

    [Fact]
    public void ReadClaims_ShouldExpandMultiValueRoleAndPermissionClaims()
    {
        // Arrange: the JWT payload collapses repeated same-type claims into a JSON array -
        // ReadClaims must expand that array back into individual Claim instances.
        var signingService = new JwtSigningService(TestJwtOptions.Create());
        var token = signingService.Generate(
            [
                new Claim(ClaimTypeConstants.UserId, "user-1"),
                new Claim(ClaimTypeConstants.Role, "admin"),
                new Claim(ClaimTypeConstants.Role, "auditor"),
                new Claim(ClaimTypeConstants.Permission, "orders.view"),
                new Claim(ClaimTypeConstants.Permission, "orders.delete"),
            ],
            DateTime.UtcNow.AddMinutes(5));

        // Act
        var claims = JwtHelper.ReadClaims(token);

        // Assert
        var roles = claims.Where(c => c.Type == ClaimTypeConstants.Role).Select(c => c.Value).ToList();
        var permissions = claims.Where(c => c.Type == ClaimTypeConstants.Permission).Select(c => c.Value).ToList();
        Assert.Equal(["admin", "auditor"], roles);
        Assert.Equal(["orders.view", "orders.delete"], permissions);
    }

    [Fact]
    public void ReadClaims_ShouldReturnEmptyList_WhenPayloadIsNull()
    {
        // Arrange: a token whose payload segment decodes to the literal JSON "null".
        var token = "header." + Convert.ToBase64String("null"u8.ToArray()).TrimEnd('=') + ".signature";

        // Act
        var claims = JwtHelper.ReadClaims(token);

        // Assert
        Assert.Empty(claims);
    }
}
