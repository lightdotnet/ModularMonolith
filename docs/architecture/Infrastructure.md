# Project Overview: Infrastructure

## Purpose

`src/Infrastructure` (assembly/namespace `StarterKit.Infrastructure`) holds the framework's **ASP.NET Core hosting building blocks**. It gives modules the types they derive from to join a host, and gives the host the registration and middleware helpers it composes:

- module registration bases (`AppModule`, `AppModuleEndpoint`) over the vendor `Lightsoft.AspNetCore.Modularity` package;
- API controller bases that carry the response envelope and a lazily resolved mediator;
- the server-side `ICurrentUser` and `IDateTime` implementations;
- controller endpoint mapping with a secure-by-default authorization requirement;
- caching, CORS, health checks, Mapster configuration, and a static bootstrap logger.

It references only `Shared`. It does not register authentication, the mediator, or validators; the host does — see [Host](Host.md).

## Public Surface

**Module registration** (`Modularity/`):

| Type | Role |
|---|---|
| `AppModule` | Abstract base over the vendor `Light.AspNetCore.Modularity.AppModule`, which declares three virtual hooks: `Add(IServiceCollection)` / `Add(IServiceCollection, IConfiguration)` for services, `Use(IApplicationBuilder)` for middleware, `Map(IEndpointRouteBuilder)` for endpoints. Adds `ShowModuleInfo`/`ShowEndpointInfo`, which log the module name through `AppLogging` when a module calls them |
| `AppModuleEndpoint` | Abstract base implementing the vendor `IModuleEndpoint` with a virtual `Map(IEndpointRouteBuilder)`, for a module's endpoints that are not mapped through its `AppModule` |

**Controller bases** (`Endpoints/`):

| Type | Role |
|---|---|
| `ApiControllerBase` | Over the vendor `Light.AspNetCore.Mvc.ApiControllerBase` (`[ApiController]`, route `api/[controller]`); adds a `Mediator` property resolved from the request services on first use |
| `VersionedApiController` | Over the vendor `VersionedApiController` (route `api/v{version:apiVersion}/[controller]`), with `[ApiVersion("1.0")]` and the same `Mediator` property |
| `BasicAuthAttribute` | Authorization filter that compares the request's Basic credentials with the `BasicAuth` configuration value in constant time and short-circuits with an `Unauthorized` result on mismatch or when the key is absent |

**Registration and middleware**:

| Member | Role |
|---|---|
| `InfrastructureModule.AddSharedInfrastructure` | Registers `IDateTime` → `DateTimeService` (singleton) and applies `MapsterSettings` |
| `InfrastructureModule.MapEndpoints(bool allowAnonymous)` | Maps controllers and requires authorization on all of them, or allows anonymous access to all when `allowAnonymous` is `true` |
| `Caching.DependencyInjection.AddAppCache` | Binds `Caching` to the vendor `CacheOptions` and calls the vendor `AddCache` |
| `Cors.DependencyInjection.AddCorsPolicy` / `UseCorsPolicy` | Registers and applies the `AllowCors` policy from `CorsOrigins` |
| `HealthChecks.Configure.AddHealthChecksService` / `MapHealthChecksEndpoint` | Adds health checks and maps `/hc` with the health-checks UI JSON writer |
| `Mappings.MapsterSettings` | Global Mapster configuration (maps `ActiveStatus` to its `State`) |
| `AppLogging` | Static Serilog logger (async console and `logs\application-startup.txt`) for startup and module-registration messages, before DI logging exists |

**Services** (`Services/`): `ServerCurrentUser` — `CurrentUserBase` over `IHttpContextAccessor.HttpContext.User`; `DateTimeService` — the `IDateTime` implementation, relying on the interface's default members. `Infrastructure` does not register `ServerCurrentUser`; the host registers it as the scoped `ICurrentUser`.

## Configuration

| Key | Read by | Meaning |
|---|---|---|
| `Caching:Provider`, `Caching:RedisHost`, `Caching:RedisPassword` | `AddAppCache` | Vendor cache provider: `memory` (default when the section is absent) or `redis`; the vendor registration throws at startup when `redis` has no `RedisHost` |
| `CorsOrigins` | `AddCorsPolicy` | Allowed origins for `AllowCors`; the vendor policy allows any method and header, with credentials |
| `BasicAuth` | `BasicAuthAttribute` | Expected Basic credentials (`user:password`) for endpoints carrying the attribute |

`AllowAnonymous` is read by the host and passed to `MapEndpoints`. Section placement per host: [Host § Configuration](Host.md#configuration).

## Usage

A module's entry point is an `AppModule` in its assembly; its controllers derive from a controller base and return through `Ok(...)`:

```csharp
public class BillingModule
    : AppModule
{
    public override void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddBillingServices(configuration);

        ShowModuleInfo();
    }
}

public class InvoiceController
    : VersionedApiController
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) =>
        Ok(await Mediator.Send(new GetInvoiceQuery(id)));
}
```

The module becomes part of a process when the host adds its assembly to the assembly scan list — see [architecture.md § Module composition](architecture.md#module-composition).

## Design Notes

- **Response envelope**: the vendor `Ok<T>(data)` wraps `data` in `Result<T>.Success`, or passes a `Result`/`Result<T>` through unchanged, stamps `RequestId` with `HttpContext.TraceIdentifier`, and converts it to an `IActionResult` whose status comes from the result. A handler that returns a failed `Result` therefore produces the matching error status through the same `Ok(...)` call. Controllers never build the envelope by hand ([CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions)).
- **Module instances per phase**: the vendor scanner creates each module through its parameterless constructor, separately for registration, middleware, and endpoint mapping, so instance state set in `Add` is not visible in `Use` or `Map`.
- **Two endpoint hooks**: `AppModule.Map` and `AppModuleEndpoint.Map` are both discovered by scanning; the host decides where each is mapped (versioned group vs. root) — see [architecture.md § Module composition](architecture.md#module-composition).
- **Secure by default**: with `allowAnonymous` `false`, every controller endpoint requires an authenticated user; an endpoint opts out with `[AllowAnonymous]`.
- **CORS without origins**: when `CorsOrigins` is absent, no policy is registered while `UseCorsPolicy` still names `AllowCors`, so no CORS headers are emitted.
- **Health checks**: `AddHealthChecksService` registers no individual check, so `/hc` reports process liveness only. `MapEndpoints`' authorization requirement applies to controllers, not to `/hc`.

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Shared` | project | `ICurrentUser`/`CurrentUserBase`, `IDateTime`, `ActiveStatus`, mediator abstractions |
| `Lightsoft.AspNetCore.Extensions` | package | Controller bases and `Result` → `IActionResult`, CORS helper, Basic-auth reader |
| `Lightsoft.AspNetCore.Modularity` | package | `AppModule`, `IModuleEndpoint`, and the assembly scanners |
| `Lightsoft.Caching` | package | `CacheOptions`, `AddCache`, `ICacheService` |
| `Lightsoft.Serilog`, `Lightsoft.FileGenerator` | package | Serilog sinks for `AppLogging`; file generation registered by the host |
| `AspNetCore.HealthChecks.UI.Client` | package | Health-check response writer |

Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](dependency-graph.md).

## Depended On By

| Project | Why |
|---|---|
| `Host` | Composes every registration and middleware helper above — see [Host](Host.md) |
| `Identity` | `IdentityModule : AppModule`, controllers on `VersionedApiController` — see [Identity](Identity.md) |
| `Identity.Web` | Its standalone host's composition (`AddSharedInfrastructure`, `AddAppCache`, `ServerCurrentUser`) |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | `AddSharedInfrastructure` for `IDateTime` in the migrate-and-seed apps |
| `tests/Framework.Tests` | Unit tests |

Its only solution reference is `Shared`, which keeps the framework's dependency direction intact.

## Notable Conventions

- Registration classes are named `DependencyInjection` per feature folder (`Caching`, `Cors`); `InfrastructureModule` and `HealthChecks.Configure` are the exceptions.
- Controller bases derive from the vendor bases rather than `ControllerBase`, so the envelope behaviour is the vendor's; check the vendor type before changing response handling.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
