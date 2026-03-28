using System.Security.Claims;

namespace Monolith.Identity.Extensions;

public sealed class ClaimComparer : IEqualityComparer<Claim>
{
    public static readonly ClaimComparer Instance = new();

    public bool Equals(Claim? x, Claim? y)
    {
        if (x is null || y is null) return false;
        return x.Type == y.Type && x.Value == y.Value;
    }

    public int GetHashCode(Claim obj)
        => HashCode.Combine(obj.Type, obj.Value);
}
