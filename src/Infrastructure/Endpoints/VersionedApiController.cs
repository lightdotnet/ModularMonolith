using Asp.Versioning;

namespace StarterKit.Infrastructure.Endpoints;

[ApiVersion("1.0")]
public abstract class VersionedApiController : Light.AspNetCore.Mvc.VersionedApiController
{
    private IMediator? _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
}
