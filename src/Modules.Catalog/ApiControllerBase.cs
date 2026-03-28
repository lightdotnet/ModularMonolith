using Light.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Monolith.Catalog;

[ApiExplorerSettings(GroupName = "Catalog")]
public abstract class ApiControllerBase : VersionedApiController
{
    private CatalogContext? _context;

    protected CatalogContext Context =>
        _context ??= HttpContext.RequestServices.GetRequiredService<CatalogContext>();
}
