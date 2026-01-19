using Microsoft.AspNetCore.Antiforgery;
using System.Runtime.CompilerServices;

namespace Monolith.Blazor;

internal class UnsafeAccessorClassAntiforgeryOptions
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "DefaultCookiePrefix")]
    public static extern ref string GetUnsafeStaticFieldDefaultCookiePrefix(AntiforgeryOptions obj);
}
