using Microsoft.AspNetCore.Http;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using System.Security.Claims;

namespace StarterKit.Infrastructure.Services;

public class ServerCurrentUser(IHttpContextAccessor httpContextAccessor)
    : CurrentUserBase, ICurrentUser
{
    public override ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
}