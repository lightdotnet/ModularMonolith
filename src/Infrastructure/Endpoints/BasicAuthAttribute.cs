using Light.AspNetCore.Authorization;
using Light.AspNetCore.Mvc;
using Light.Contracts;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace StarterKit.Infrastructure.Endpoints;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class BasicAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var key = configuration.GetValue<string>("BasicAuth");

        var authFromRequest = context.HttpContext.Request.ReadBasicAuthorization();

        var isRequestValid = key is not null && IsMatch(authFromRequest.ToString(), key);

        if (!isRequestValid)
        {
            context.Result = Result.Unauthorized().ToActionResult();
            return;
        }
    }

    private static bool IsMatch(string? value, string key) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(value ?? string.Empty),
            Encoding.UTF8.GetBytes(key));
}
