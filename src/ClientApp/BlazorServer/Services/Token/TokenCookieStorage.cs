namespace Monolith.Blazor.Services.Token;

public class TokenCookieStorage(IHttpContextAccessor httpContextAccessor) : TokenStorage
{
    public override Task<TokenModel?> GetAsync()
    {
        if (httpContextAccessor.HttpContext is HttpContext httpContext)
        {
            if (httpContext.Items.TryGetValue(Constants.TokenCookieName, out var tokenObj) && tokenObj is TokenModel token)
            {
                return Task.FromResult<TokenModel?>(token);
            }

            var tokenCookieValue = httpContext.Request.Cookies[Constants.TokenCookieName];

            var tokenData = TokenModel.ReadFrom(tokenCookieValue);

            Console.WriteLine("Token loaded from Cookies");

            return Task.FromResult(tokenData);
        }

        return Task.FromResult<TokenModel?>(default);
    }

    public override Task SaveAsync(TokenModel token)
    {
        if (httpContextAccessor.HttpContext is HttpContext httpContext)
        {
            // Cookies written with Response.Cookies.Append
            //      are NOT available in Request.Cookies during the same request.
            //      So we store it in HttpContext.Items for immediate access.
            httpContext.Items[Constants.TokenCookieName] = token;

            httpContext.Response.Cookies.Append(
                Constants.TokenCookieName,
                token.ToString(),
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
