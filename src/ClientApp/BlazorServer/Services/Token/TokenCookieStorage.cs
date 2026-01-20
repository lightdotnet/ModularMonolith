using Microsoft.AspNetCore.DataProtection;

namespace Monolith.Blazor.Services.Token;

public class TokenCookieStorage(
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider) : TokenStorage
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("jwt");

    private TokenModel? Unprotect(string tokenString)
    {
        try
        {
            var unprotectedData = _protector.Unprotect(tokenString);
            return TokenModel.ReadFrom(unprotectedData);
        }
        catch
        {
            return null;
        }
    }

    public override Task<TokenModel?> GetAsync()
    {
        if (httpContextAccessor.HttpContext is HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(Constants.TokenCookieName, out var cookieItemValue) && cookieItemValue is string token)
            {
                return Task.FromResult(Unprotect(token));
            }

            var tokenCookieValue = httpContext.Request.Cookies[Constants.TokenCookieName];

            if (!string.IsNullOrEmpty(tokenCookieValue))
            {
                Console.WriteLine("Token loaded from Cookies");

                return Task.FromResult(Unprotect(tokenCookieValue));
            }
        }

        return Task.FromResult<TokenModel?>(default);
    }

    public override Task SaveAsync(TokenModel token)
    {
        if (httpContextAccessor.HttpContext is HttpContext httpContext)
        {
            var protectedValue = _protector.Protect(token.ToString());

            // Cookies written with Response.Cookies.Append
            //      are NOT available in Request.Cookies during the same request.
            //      So we store it in HttpContext.Items for immediate access.
            httpContext.Items[Constants.TokenCookieName] = protectedValue;

            httpContext.Response.Cookies.Append(
                Constants.TokenCookieName,
                protectedValue,
                new CookieOptions
                {
                    Expires = token.ExpireOn,
                    SameSite = SameSiteMode.Strict,
                    Secure = httpContext.Request.IsHttps,
                    HttpOnly = true
                });
        }

        return Task.CompletedTask;
    }

    public override Task ClearAsync()
    {
        if (httpContextAccessor.HttpContext is HttpContext httpContext)
        {
            httpContext.Response.Cookies.Delete(Constants.TokenCookieName);
        }

        return Task.CompletedTask;
    }
}
