using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Authentication.Jwt;

namespace Identity.Tests.TestSupport;

public static class TestJwtOptions
{
    public static IOptions<JwtOptions> Create(
        string issuer = "https://identity.tests",
        string secretKey = "unit-test-signing-key-please-keep-32chars+",
        int accessTokenExpirationSeconds = 3600,
        int refreshTokenExpirationDays = 7) =>
        Options.Create(new JwtOptions
        {
            Issuer = issuer,
            SecretKey = secretKey,
            AccessTokenExpirationSeconds = accessTokenExpirationSeconds,
            RefreshTokenExpirationDays = refreshTokenExpirationDays,
        });
}
