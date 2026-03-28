using Microsoft.Extensions.Logging;
using Monolith.Authorization;
using System.Reflection;
using System.Security.Claims;

namespace Monolith.Services;

public class AppPermissionManager(ILogger<AppPermissionManager> logger)
{
    private readonly string _permissionClaimType = Monolith.Claims.ClaimTypes.Permission;

    private IEnumerable<Claim>? Claims { get; set; }

    private IEnumerable<Claim> GetClaims()
    {
        if (Claims is null)
        {
            var fromClass = typeof(Permissions);

            var claims = new List<Claim>();

            // get classes in class
            var modules = fromClass.GetNestedTypes();

            foreach (var module in modules)
            {
                // get props in class
                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                foreach (FieldInfo fi in fields)
                {
                    var propertyValue = fi.GetValue(null)?.ToString();

                    if (propertyValue != null)
                    {
                        claims.Add(new Claim(_permissionClaimType, propertyValue));
                    }
                    //TODO - take descriptions from description attribute
                }
            }

            logger.LogWarning("AppPermissionManager initialized with {count} claims.", claims.Count);

            Claims = claims;
        }

        return Claims;
    }

    public Task<bool> IsValidAsync(string permission)
    {
        var result = GetClaims()?.Any(x => x.Type == _permissionClaimType && x.Value == permission);

        return Task.FromResult(result is true);
    }
}
