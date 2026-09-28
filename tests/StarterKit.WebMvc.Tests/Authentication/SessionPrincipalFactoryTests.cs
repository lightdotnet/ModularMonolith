using System.Security.Claims;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Tests.TestSupport;
using Xunit;

namespace StarterKit.WebMvc.Tests.Authentication;

public class SessionPrincipalFactoryTests
{
    private static readonly string Token = TestJwt.Create(new Dictionary<string, object>
    {
        ["uid"] = "user-1",
        ["un"] = "alice",
        ["jti"] = "session-1",
        ["role"] = new[] { "Editor" },
        ["permission"] = new[] { "identity.users.view" },
        ["employee_id"] = "emp-42",
    });

    [Fact]
    public void Create_ShouldBuildAuthenticatedPrincipalFromTokenAndProfile()
    {
        // Arrange
        var profile = new UserDto
        {
            Id = "user-1",
            UserName = "alice",
            FirstName = "Alice",
            LastName = "Liddell",
            Email = "alice@example.com",
            AuthProvider = "Local",
            Claims =
            [
                new ClaimDto
                {
                    Type = "department",
                    Value = "Sales",
                },
            ],
        };

        // Act
        var principal = SessionPrincipalFactory.Create(
            Token,
            profile);

        // Assert
        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.Equal(SessionDefaults.AuthenticationScheme, principal.Identity.AuthenticationType);
        Assert.Equal("alice", principal.Identity.Name);
        Assert.True(principal.IsInRole("Editor"));
        Assert.Equal("user-1", principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("session-1", principal.FindFirstValue(SessionClaimTypes.TokenId));
        Assert.Equal("emp-42", principal.FindFirstValue(SessionClaimTypes.EmployeeId));
        Assert.Equal("Alice", principal.FindFirstValue(SessionClaimTypes.FirstName));
        Assert.Equal("Liddell", principal.FindFirstValue(SessionClaimTypes.LastName));
        Assert.Equal("alice@example.com", principal.FindFirstValue(SessionClaimTypes.Email));
        Assert.Equal("Local", principal.FindFirstValue(SessionClaimTypes.AuthProvider));
        Assert.Equal("Sales", principal.FindFirstValue("department"));
    }

    [Fact]
    public void Create_ProfileClaims_ShouldNotShadowOrAddIdentityOrAuthorizationClaims()
    {
        // Arrange
        var profile = new UserDto
        {
            Id = "user-1",
            UserName = "alice",
            Claims =
            [
                Claim(SessionClaimTypes.UserId, "attacker"),
                Claim(SessionClaimTypes.UserName, "root"),
                Claim(SessionClaimTypes.TokenId, "other-session"),
                Claim(SessionClaimTypes.Role, "Admin"),
                Claim(SessionClaimTypes.Permission, "identity.users.delete"),
                Claim(ClaimTypes.NameIdentifier, "attacker"),
                Claim(ClaimTypes.Name, "root"),
                Claim(ClaimTypes.Role, "Admin"),
            ],
        };

        // Act
        var principal = SessionPrincipalFactory.Create(
            Token,
            profile);

        // Assert: only the token-sourced values are present.
        Assert.Equal(["user-1"], Values(principal, SessionClaimTypes.UserId));
        Assert.Equal(["alice"], Values(principal, SessionClaimTypes.UserName));
        Assert.Equal(["session-1"], Values(principal, SessionClaimTypes.TokenId));
        Assert.Equal(["Editor"], Values(principal, SessionClaimTypes.Role));
        Assert.Equal(["identity.users.view"], Values(principal, SessionClaimTypes.Permission));
        Assert.Equal(["user-1"], Values(principal, ClaimTypes.NameIdentifier));
        Assert.Empty(Values(principal, ClaimTypes.Name));
        Assert.Empty(Values(principal, ClaimTypes.Role));
        Assert.False(principal.IsInRole("Admin"));
        Assert.Equal("alice", principal.Identity!.Name);
    }

    [Fact]
    public void Create_RetainedClaims_ShouldBeFilteredLikeProfileClaims()
    {
        // Arrange
        var retained = new[]
        {
            new Claim(SessionClaimTypes.FirstName, "Alice"),
            new Claim(SessionClaimTypes.Permission, "identity.users.delete"),
            new Claim(SessionClaimTypes.UserId, "attacker"),
        };

        // Act
        var principal = SessionPrincipalFactory.Create(
            Token,
            profile: null,
            retainedClaims: retained);

        // Assert
        Assert.Equal("Alice", principal.FindFirstValue(SessionClaimTypes.FirstName));
        Assert.Equal(["identity.users.view"], Values(principal, SessionClaimTypes.Permission));
        Assert.Equal(["user-1"], Values(principal, SessionClaimTypes.UserId));
    }

    [Fact]
    public void Create_ShouldDeduplicateIdenticalClaims()
    {
        // Arrange
        var profile = new UserDto
        {
            Id = "user-1",
            UserName = "alice",
            Claims =
            [
                Claim("employee_id", "emp-42"),
            ],
        };

        // Act
        var principal = SessionPrincipalFactory.Create(
            Token,
            profile);

        // Assert
        Assert.Equal(["emp-42"], Values(principal, SessionClaimTypes.EmployeeId));
    }

    [Fact]
    public void NonTokenClaims_ShouldReturnOnlyProfileSourcedClaims()
    {
        // Arrange
        var profile = new UserDto
        {
            Id = "user-1",
            UserName = "alice",
            FirstName = "Alice",
        };

        var principal = SessionPrincipalFactory.Create(
            Token,
            profile);

        // Act
        var extra = SessionPrincipalFactory.NonTokenClaims(
            principal,
            Token);

        // Assert
        var claim = Assert.Single(extra);
        Assert.Equal(SessionClaimTypes.FirstName, claim.Type);
        Assert.Equal("Alice", claim.Value);
    }

    private static ClaimDto Claim(
        string type,
        string value) =>
        new()
        {
            Type = type,
            Value = value,
        };

    private static string[] Values(
        ClaimsPrincipal principal,
        string type) =>
        principal.FindAll(type)
            .Select(claim => claim.Value)
            .ToArray();
}
