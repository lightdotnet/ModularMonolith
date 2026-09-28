using System.Globalization;
using Microsoft.AspNetCore.Authentication;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// The session state persisted in the (Data-Protection-encrypted) cookie's
/// <see cref="AuthenticationProperties"/> — the equivalent of the admin client's <c>StoredSession</c>.
/// </summary>
internal sealed record SessionTicket(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset SessionExpiresAt,
    int RefreshFailureCount)
{
    public static SessionTicket? Read(AuthenticationProperties properties)
    {
        var accessToken = properties.GetTokenValue(SessionDefaults.AccessTokenName);

        if (string.IsNullOrEmpty(accessToken)
            || !TryParseDate(properties.GetTokenValue(SessionDefaults.AccessTokenExpiresAtName), out var accessTokenExpiresAt)
            || !TryParseDate(properties.GetString(SessionDefaults.SessionExpiresAtItem), out var sessionExpiresAt))
        {
            return null;
        }

        _ = int.TryParse(
            properties.GetString(SessionDefaults.RefreshFailureCountItem),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var refreshFailureCount);

        return new SessionTicket(
            accessToken,
            properties.GetTokenValue(SessionDefaults.RefreshTokenName),
            accessTokenExpiresAt,
            sessionExpiresAt,
            refreshFailureCount);
    }

    /// <summary>
    /// Writes this state onto <paramref name="properties"/>. <c>IssuedUtc</c>/<c>ExpiresUtc</c> are
    /// pinned so the cookie always expires at the hard session cap: when the cookie is renewed the
    /// handler re-derives expiry as <c>now + (ExpiresUtc - IssuedUtc)</c>, which would otherwise
    /// silently extend the session on every refresh.
    /// </summary>
    public void WriteTo(
        AuthenticationProperties properties,
        DateTimeOffset now)
    {
        var tokens = new List<AuthenticationToken>
        {
            new() { Name = SessionDefaults.AccessTokenName, Value = AccessToken },
            new() { Name = SessionDefaults.AccessTokenExpiresAtName, Value = FormatDate(AccessTokenExpiresAt) },
        };

        if (!string.IsNullOrEmpty(RefreshToken))
        {
            tokens.Add(new AuthenticationToken { Name = SessionDefaults.RefreshTokenName, Value = RefreshToken });
        }

        properties.StoreTokens(tokens);

        properties.SetString(
            SessionDefaults.SessionExpiresAtItem,
            FormatDate(SessionExpiresAt));

        properties.SetString(
            SessionDefaults.RefreshFailureCountItem,
            RefreshFailureCount.ToString(CultureInfo.InvariantCulture));

        properties.IsPersistent = true;
        properties.AllowRefresh = true;
        properties.IssuedUtc = now;
        properties.ExpiresUtc = SessionExpiresAt;
    }

    private static string FormatDate(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static bool TryParseDate(
        string? value,
        out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out result);
    }
}
