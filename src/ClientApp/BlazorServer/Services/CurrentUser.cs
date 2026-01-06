using Monolith.Authorization;
using System.Security.Claims;

namespace Monolith.Blazor.Services;

public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : CurrentUserBase, IClientCurrentUser
{
    public override ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
}

