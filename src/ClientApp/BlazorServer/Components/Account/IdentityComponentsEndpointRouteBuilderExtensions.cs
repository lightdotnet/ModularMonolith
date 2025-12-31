using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Monolith.Blazor.Services;
using Monolith.Blazor.Services.Token;

namespace Monolith.Blazor.Components.Account;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/account");

        accountGroup.MapGet("/logout", async (
            HttpContext httpContext,
            TokenStorage tokenStorage,
            [FromQuery] string? returnUrl) =>
        {
            await httpContext.SignOutAsync();

            await httpContext.RequestServices.GetRequiredService<TokenStorage>().ClearAsync();

            HttpContextEntensions.ClearCookies(httpContext);

            return TypedResults.LocalRedirect($"~/account/login?returnUrl={returnUrl}");
        });

        return accountGroup;
    }
}
