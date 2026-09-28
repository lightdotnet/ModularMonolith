using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using StarterKit.WebMvc.Authentication;
using Xunit;

namespace StarterKit.WebMvc.Tests.Authentication;

public class ExternalLoginPkceTests
{
    // RFC 7636 Appendix B.
    private const string RfcVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    private const string RfcChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    [Fact]
    public void CreatePair_Verifier_ShouldBe43UnreservedBase64UrlCharacters()
    {
        // Act
        var (verifier, _) = ExternalLoginPkce.CreatePair();

        // Assert: 32 random bytes -> 43 base64url chars, within RFC 7636's 43..128 range.
        Assert.Equal(43, verifier.Length);
        Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), verifier);
    }

    [Fact]
    public void CreatePair_ShouldProduceDistinctVerifiers()
    {
        // Act
        var first = ExternalLoginPkce.CreatePair();
        var second = ExternalLoginPkce.CreatePair();

        // Assert
        Assert.NotEqual(first.Verifier, second.Verifier);
    }

    [Fact]
    public void CreatePair_Challenge_ShouldBeS256OfVerifier()
    {
        // Act
        var (verifier, challenge) = ExternalLoginPkce.CreatePair();

        // Assert
        Assert.Equal(S256(verifier), challenge);
        Assert.Equal(43, challenge.Length);
        Assert.DoesNotContain("=", challenge);
    }

    [Fact]
    public void S256Reference_ShouldMatchRfc7636TestVector()
    {
        // Anchors the S256 derivation the test above checks CreatePair against to the RFC vector.
        Assert.Equal(RfcChallenge, S256(RfcVerifier));
    }

    private static string S256(string verifier) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
}
