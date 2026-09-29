using Microsoft.AspNetCore.Routing;

namespace StarterKit.Infrastructure.Modularity;

public abstract class AppModuleEndpoint : Light.AspNetCore.Modularity.IModuleEndpoint
{
    public virtual void Map(IEndpointRouteBuilder endpoints)
    { }
}
