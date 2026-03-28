using Xunit;
using System.Collections.Generic;
using System.Security.Claims;
using Monolith.Claims;

namespace Shared.Tests.Claims;

public class ClaimExtensionsTests
{
    [Fact]
    public void Add_ShouldAddClaim_WhenValueIsNotNullOrEmpty()
    {
        // Arrange
        var claims = new List<Claim>();
        string key = "key";
        string value = "value";

        // Act
        claims.Add(key, value);

        // Assert
        Assert.Single(claims);
        Assert.Equal(key, claims[0].Type);
        Assert.Equal(value, claims[0].Value);
    }

    [Fact]
    public void Add_ShouldNotAddClaim_WhenValueIsNullOrEmpty()
    {
        // Arrange
        var claims = new List<Claim>();
        string key = "key";
        string? value = null;

        // Act
        claims.Add(key, value);

        // Assert
        Assert.Empty(claims);
    }
}