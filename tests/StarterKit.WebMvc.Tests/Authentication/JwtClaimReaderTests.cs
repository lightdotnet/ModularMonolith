using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Tests.TestSupport;
using Xunit;

namespace StarterKit.WebMvc.Tests.Authentication;

public class JwtClaimReaderTests
{
    [Fact]
    public void Read_ShouldFlattenStringAndArrayClaims()
    {
        // Arrange
        var token = TestJwt.Create(new Dictionary<string, object>
        {
            ["uid"] = "user-1",
            ["un"] = "alice",
            ["jti"] = "session-1",
            ["role"] = new[] { "Admin", "Editor" },
            ["permission"] = new[] { "identity.users.view", "identity.users.edit" },
            ["employee_id"] = "emp-42",
            ["exp"] = 1_900_000_000,
        });

        // Act
        var claims = JwtClaimReader.Read(token);

        // Assert
        Assert.Equal("user-1", Single(claims, SessionClaimTypes.UserId));
        Assert.Equal("alice", Single(claims, SessionClaimTypes.UserName));
        Assert.Equal("session-1", Single(claims, SessionClaimTypes.TokenId));
        Assert.Equal("emp-42", Single(claims, SessionClaimTypes.EmployeeId));
        Assert.Equal(["Admin", "Editor"], Values(claims, SessionClaimTypes.Role));
        Assert.Equal(["identity.users.view", "identity.users.edit"], Values(claims, SessionClaimTypes.Permission));

        // Numeric claims (exp/iat) are skipped.
        Assert.DoesNotContain(claims, claim => claim.Type == "exp");
    }

    [Fact]
    public void Read_ShouldReadSingleValuedRoleAndPermissionAsOneClaim()
    {
        // Arrange
        var token = TestJwt.Create(new Dictionary<string, object>
        {
            ["uid"] = "user-1",
            ["role"] = "Admin",
            ["permission"] = "identity.users.view",
        });

        // Act
        var claims = JwtClaimReader.Read(token);

        // Assert
        Assert.Equal(["Admin"], Values(claims, SessionClaimTypes.Role));
        Assert.Equal(["identity.users.view"], Values(claims, SessionClaimTypes.Permission));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("header.%%%notbase64%%%.sig")]
    [InlineData("eyJhbGciOiJub25lIn0.bm90LWpzb24.sig")]
    [InlineData("eyJhbGciOiJub25lIn0.WzEsMl0.sig")]
    public void Read_ShouldReturnNoClaims_ForMalformedTokens(string token)
    {
        // Act
        var claims = JwtClaimReader.Read(token);

        // Assert
        Assert.Empty(claims);
    }

    private static string Single(
        IEnumerable<System.Security.Claims.Claim> claims,
        string type) =>
        Assert.Single(claims, claim => claim.Type == type).Value;

    private static string[] Values(
        IEnumerable<System.Security.Claims.Claim> claims,
        string type) =>
        claims
            .Where(claim => claim.Type == type)
            .Select(claim => claim.Value)
            .ToArray();
}
