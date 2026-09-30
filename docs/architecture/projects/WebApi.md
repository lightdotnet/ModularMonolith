# Project Overview: StarterKit.WebApi

## Purpose

`src/StarterKit.WebApi` (assembly/namespace `StarterKit.WebApi`) is the **composition root and the only deployable** in the solution. It composes the framework projects and the modules (Identity, Notifications) into one ASP.NET Core process that serves:

- the versioned JSON API (`/api/v{version}/…`) contributed by modules, authenticated with Bearer tokens;
- the Notifications SignalR hub, authenticated with the Identity hub token;
- the Identity Razor Pages (`/Account/…`, `/Admin/…`, `/signin-oidc`) contributed by `Identity.Web`, authenticated with the Identity application cookie.

It holds no business logic: everything it registers comes from a framework project, a module, or the Aspire service defaults. For local development it can also run under the Aspire app host — see [Aspire](Aspire.md).

## Public Surface

The host is an application, not a library; its surface is its composition.

| Area | Role |
|---|---|
| `Program` | Clears the default logging providers, applies the Aspire service defaults (`AddServiceDefaults`), configures Serilog with `writeToProviders: true` (logging details in [Aspire § How StarterKit.WebApi Uses the Service Defaults](Aspire.md#how-starterkitwebapi-uses-the-service-defaults)), calls `ConfigureServices`, adds lowercase controllers and the default JSON / invalid-model-state handling, then `ConfigurePipelines`, WebSockets, endpoint mapping, and `MapDefaultEndpoints`. `AllowAnonymous` (configuration) is passed to endpoint mapping. |
| `ConfigureExtensions.ConfigureServices` | Registers everything discovered from the **assembly scan list** — the host, Identity, and Notifications assemblies ([architecture.md § Module composition](../architecture.md#module-composition)) — the shared infrastructure (exception handler, API versioning, Swagger, caching, health checks, CORS, current user, permission policies), the rate limiter, `AddIdentityWeb`, and `AddApiAuthentication`. |
| `ConfigureExtensions.ConfigurePipelines` | Builds the middleware pipeline and maps health checks, module endpoints, and the Identity Razor Pages — order in [architecture.md § HTTP request pipeline](../architecture.md#http-request-pipeline). |
| `Authentication.ApiAuthenticationExtensions.AddApiAuthentication` | Co-host authentication: the Bearer scheme, a hub-only `HubBearer` scheme, and the `Identity.CookieOrBearer` policy scheme described under [Design Notes](#design-notes). |
| `Authentication.HubTokenApiGuardHandler` | Authorization handler that fails any request outside the hub path carrying a hub token — a backstop behind the `Bearer` scheme's audience check. |

Health endpoints: `/hc` in every environment (the deployment health endpoint), plus `/health` and `/alive` in `Development` only for Aspire — see [Aspire § How StarterKit.WebApi Uses the Service Defaults](Aspire.md#how-starterkitwebapi-uses-the-service-defaults).

## Configuration

Top-level sections the host reads directly or passes to the projects it composes (`src/StarterKit.WebApi/appsettings*.json`):

| Section | Owner |
|---|---|
| `DbProvider`, `ConnectionStrings` | `Persistence` — the Identity and Notifications contexts use `DefaultConnection` |
| `Jwt` | Identity module (token issuance) and the host's Bearer schemes; required — startup fails if it is missing or has no `SecretKey` |
| `Authentication:Microsoft`, `ExternalLoginRelay`, `IdentityWeb` | Identity / Identity.Web — see [Identity](Identity.md) |
| `MemberOfDomain` | Identity (Active Directory) |
| `Notifications:Hub:Path` | Notifications — hub path, also used by the host's scheme routing; see [Notifications § Configuration](Notifications.md#configuration) |
| `SmtpMail` | Notifications — SMTP server, required; see [Notifications § Configuration](Notifications.md#configuration). The checked-in values point at a test SMTP host with empty credentials |
| `RabbitMQ` | [EventBusMassTransitRabbitMQ § Configuration](EventBusMassTransitRabbitMQ.md#configuration) |
| `CorsOrigins`, `Caching`, `Swagger`, `RequestLogging`, `Serilog` | Framework infrastructure and vendor packages |
| `AllowAnonymous` | `StarterKit.WebApi` — endpoint mapping |
| `BasicAuth` | [Infrastructure](Infrastructure.md) — `BasicAuthAttribute` credentials |

Default values and local provider switching: [development-guide.md § Running Locally](../../conventions/development-guide.md#running-locally). The Aspire app host overrides `Caching` and `RabbitMQ` — see [Aspire § Running](Aspire.md#running).

## Design Notes

- **Scheme routing by path** (`Identity.CookieOrBearer` is the default authenticate/challenge scheme):
  - hub path → `HubBearer` — validates issuer, signing key, and the hub audience; reads the token from `?access_token=` for WebSocket requests;
  - `/api` → `Bearer` — post-configured to reject any token carrying the hub audience, so a hub token cannot call the API;
  - everything else → the Identity application cookie (Razor Pages, `/signin-oidc`).
  Cookie login/access-denied redirects are turned into `401`/`403` for `/api` and hub requests. Both Bearer schemes copy the token expiry into the auth ticket, so the hub can close a connection when its token expires.
- **Hub path**: the host reads `Notifications:Hub:Path` once at registration and checks it with the Notifications module's `NotificationHubOptionsSetup.IsValidHubPath`, failing startup on an invalid value; it also registers the options through the same `AddNotificationHubOptions` the module uses, so the hub mapping and the scheme routing share one validated value — see [Notifications § Design Notes](Notifications.md#design-notes).
- **Rate limiting**: the `external-login` policy is a fixed window of 10 requests per minute per remote IP, with no queue. It guards the anonymous external-login relay pages and `POST auth/token/external`.
- **Standalone alternative**: `Identity.Web` can also run as its own cookie-only host; that host does not call `AddApiAuthentication` — see [Identity](Identity.md).

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Infrastructure` | project | Hosting building blocks, module registration, CORS, caching, health checks, current user |
| `Persistence` | project | EF Core provider configuration used by module contexts |
| `EventBusMassTransitRabbitMQ` | project | `AddEventBus` |
| `Identity` | project | `IdentityModule` and its assembly in the scan list; JWT options for the Bearer schemes |
| `Identity.Web` | project | `AddIdentityWeb` / `UseIdentityWeb` (Razor Pages) |
| `Notifications` | project | `NotificationsModule` and its assembly in the scan list; `NotificationHubOptions` and `NotificationHubOptionsSetup` for the hub path |
| `StarterKit.ServiceDefaults` | project | `AddServiceDefaults` / `MapDefaultEndpoints` — see [Aspire](Aspire.md) |
| `Lightsoft.AspNetCore.Extensions`, `Lightsoft.AspNetCore.Swagger` | package | Vendor host helpers (JWT auth, exception handling, request logging, Swagger) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | package | `HubBearer` scheme |
| `FluentValidation.DependencyInjectionExtensions`, `AspNetCore.HealthChecks.UI.Client`, `Spectre.Console` | package | Validator registration, health-check output, startup banner |

Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](../dependency-graph.md).

## Depended On By

`StarterKit.AppHost` references it, to run it as the `api` resource — see [Aspire](Aspire.md). No other project references it. Tests do not reference it.

## Notable Conventions

- The host composes through `ConfigureExtensions` (`ConfigureServices`/`ConfigurePipelines`) rather than a `DependencyInjection` class; its only other registration class is `ApiAuthenticationExtensions`, which is co-host-only.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
