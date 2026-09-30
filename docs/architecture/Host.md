# Project Overview: Host

## Purpose

`src/Host` (assembly/namespace `StarterKit.Host`) is the **composition root and the only deployable** in the solution. It composes the framework projects and the Identity module into one ASP.NET Core process that serves both:

- the versioned JSON API (`/api/v{version}/…`) contributed by modules, authenticated with Bearer tokens;
- the Identity Razor Pages (`/Account/…`, `/signin-oidc`) contributed by `Identity.Web`, authenticated with the Identity application cookie.

It holds no business logic: everything it registers comes from a framework project or a module.

## Public Surface

The host is an application, not a library; its surface is its composition.

| Area | Role |
|---|---|
| `Program` | Configures Serilog, calls `ConfigureServices`, adds lowercase controllers and the default JSON / invalid-model-state handling, then `ConfigurePipelines` and endpoint mapping. `AllowAnonymous` (configuration) is passed to endpoint mapping. |
| `ConfigureExtensions.ConfigureServices` | Registers, over one **assembly scan list** (the host assembly and the Identity module assembly): FluentValidation validators, the mediator with the logging and validation behaviours, the event bus (`AddEventBus`), and module registration (`AddModules<AppModule>`). Also adds the shared infrastructure (exception handler, API versioning, Swagger, caching, health checks, CORS, current user, permission policies), the rate limiter, `AddIdentityWeb`, and `AddApiAuthentication`. |
| `ConfigureExtensions.ConfigurePipelines` | Middleware order: trace id → exception handler → request logging → static files → routing → CORS → rate limiter → authentication → authorization → Swagger; then health checks, `UseModules`, module endpoints (unversioned and under `api/v{version:apiVersion}`), and `UseIdentityWeb` (Razor Pages). |
| `Authentication.ApiAuthenticationExtensions.AddApiAuthentication` | Co-host authentication: the Bearer scheme, a hub-only `HubBearer` scheme, and the `Identity.CookieOrBearer` policy scheme described under [Design Notes](#design-notes). |

## Configuration

Top-level sections the host reads directly or passes to the projects it composes (`src/Host/appsettings*.json`):

| Section | Owner |
|---|---|
| `DbProvider`, `ConnectionStrings` | `Persistence` — the Identity context uses `DefaultConnection` |
| `Jwt` | Identity module (token issuance) and the host's Bearer schemes; required — startup fails if it is missing or has no `SecretKey` |
| `Authentication:Microsoft`, `ExternalLoginRelay` | Identity / Identity.Web — see [Identity](Identity.md) |
| `MemberOfDomain` | Identity (Active Directory) |
| `RabbitMQ` | [EventBusMassTransitRabbitMQ § Configuration](EventBusMassTransitRabbitMQ.md#configuration) |
| `CorsOrigins`, `Caching`, `Swagger`, `RequestLogging`, `Serilog` | Framework infrastructure and vendor packages |
| `AllowAnonymous` | Host — endpoint mapping |
| `BasicAuth` | [Infrastructure](Infrastructure.md) — `BasicAuthAttribute` credentials |
| `Notifications:Hub:Path` | Host — hub path used by the authentication scheme routing |

The `Development` settings do not override `DbProvider`, so the host uses the provider from `appsettings.json` (`MSSQL`, SQL Server LocalDB) unless it is overridden. The host does not migrate at startup: a relational database gets its schema from the migrator projects — see [migrations.md](../conventions/migrations.md). `RabbitMQ:Enable` is `false` by default, so no broker is needed. Local setup and provider switching: [development-guide.md](../conventions/development-guide.md).

## Design Notes

- **Scheme routing by path** (`Identity.CookieOrBearer` is the default authenticate/challenge scheme):
  - hub path → `HubBearer` — validates issuer, signing key, and the hub audience; reads the token from `?access_token=` for WebSocket requests;
  - `/api` → `Bearer` — post-configured to reject any token carrying the hub audience, so a hub token cannot call the API;
  - everything else → the Identity application cookie (Razor Pages, `/signin-oidc`).
  Cookie login/access-denied redirects are turned into `401`/`403` for `/api` and hub requests. Both Bearer schemes copy the token expiry into the auth ticket.
- **Hub path**: no project in this solution maps a SignalR hub; the `Notifications:Hub` section and `HubOptions` are a host-local stand-in that only drives scheme routing. Which module will own the hub is unknown on this branch.
- **Rate limiting**: the `external-login` policy is a fixed window of 10 requests per minute per remote IP, with no queue. It guards the anonymous external-login relay pages and `POST auth/token/external`.
- **Assembly scan list**: a new module becomes part of the process by adding its assembly to the list in `ConfigureExtensions`; validators, mediator handlers, modules, module endpoints, and event-bus consumers are all discovered from that one list.
- **Standalone alternative**: `Identity.Web` can also run as its own cookie-only host; that host does not call `AddApiAuthentication` — see [Identity](Identity.md).

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Infrastructure` | project | Hosting building blocks, module registration, CORS, caching, health checks, current user |
| `Persistence` | project | EF Core provider configuration used by module contexts |
| `EventBusMassTransitRabbitMQ` | project | `AddEventBus` |
| `Identity` | project | `IdentityModule` and its assembly in the scan list; JWT options for the Bearer schemes |
| `Identity.Web` | project | `AddIdentityWeb` / `UseIdentityWeb` (Razor Pages) |
| `Lightsoft.AspNetCore.Extensions`, `Lightsoft.AspNetCore.Swagger` | package | Vendor host helpers (JWT auth, exception handling, request logging, Swagger) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | package | `HubBearer` scheme |
| `FluentValidation.DependencyInjectionExtensions`, `AspNetCore.HealthChecks.UI.Client`, `Spectre.Console` | package | Validator registration, health-check output, startup banner |

Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](dependency-graph.md).

## Depended On By

No project references it. Tests do not reference it.

## Notable Conventions

- The host composes through `ConfigureExtensions` (`ConfigureServices`/`ConfigurePipelines`) rather than a `DependencyInjection` class; its only other registration class is `ApiAuthenticationExtensions`, which is co-host-only.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
