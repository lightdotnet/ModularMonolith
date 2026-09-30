# Project Overview: Identity

The Identity module is three projects forming one bounded context:

| Project | Assembly | Role |
|---|---|---|
| `src/Identity.Contracts` | `StarterKit.Modules.Identity.Contracts` | The cross-module seam — the only Identity project another module may reference |
| `src/Identity` | `StarterKit.Modules.Identity` | The module implementation |
| `src/Identity.Web` | `StarterKit.Modules.Identity.Web` | Razor Pages for cookie login and the Microsoft external-login relay |

## Purpose

The module owns users, roles, claims, and sessions, and issues the tokens the rest of the solution authenticates with:

- an ASP.NET Core Identity store (`User`/`Role` and their claim/login/token entities, plus `UserSession`) in the module's own EF Core context;
- self-issued JWTs: an access/refresh token pair bound to a user session, and a short-lived hub token carrying a dedicated hub audience;
- password login, optional Active Directory lookups, and optional Microsoft Entra ID (OIDC) external login with link-or-provision of the local user;
- user, role, permission, and user-profile endpoints for the JSON API;
- a permission catalog registered with the framework's permission authorization.

## Public Surface

**`Identity.Contracts`**:

| Type | Role |
|---|---|
| `IIdentityModuleApi` | In-process seam other modules use to query users (by id, ids, email, or granted permission) and to idempotently ensure a user exists by email |
| `UserSummary` | The user shape exposed through the seam |
| `IntegrationEvents/*` | Integration events (`IntegrationEvent` records) published when a user is provisioned, updated, has its status changed, or is deleted. Each carries the user's full current state and a `Version`; consumers must be idempotent on the user id and drop events that are not newer than the one already applied |

**`Identity`**:

| Area | Role |
|---|---|
| `IdentityModule` | The module's `AppModule`: registers the Identity services, JWT token services, and the permission provider |
| `DependencyInjection.AddIdentityServices` | Identity store over `IdentityDbContext`, Active Directory (a fake service unless `MemberOfDomain` is set and the OS is Windows), user/role/external-login services, the external-login code store, the integration-event collector, and `IIdentityModuleApi` |
| `Endpoints/` | Controllers on `VersionedApiController`. `TokenController` (`api/v{version}/auth`) exposes `token/get`, `token/refresh`, `token/external` (anonymous) and `token/hub` (authenticated); the other controllers manage users, roles, permissions, and the current user's profile and sessions |

Visibility inside `Identity`: the domain entities, request/response models, options and result types, and service interfaces are `public`; their implementations, `IdentityDbContext` and its initialiser, the token services, the `IIdentityModuleApi` implementation, the permission catalog, the CQRS commands/handlers, and the integration-event buffering are `internal`. `InternalsVisibleTo` grants `Identity.Tests`, `Identity.Web`, and the three migration projects.

**`Identity.Web`**:

| Type | Role |
|---|---|
| `DependencyInjection.AddIdentityWeb` / `UseIdentityWeb` | Application and external cookies, the `SignInManager`, the optional `Microsoft` OIDC scheme, and Razor Pages |
| `Pages/Account/*` | Interactive login/logout and Microsoft login, plus the relay pair `ExternalLoginStart` / `ExternalLoginRelay` for separate-origin clients |

## Configuration

| Section | Purpose |
|---|---|
| `Jwt` | Issuer, signing key, token lifetimes, hub audience |
| `Authentication:Microsoft` | Entra ID client id/secret, instance, callback path, allow-listed tenant ids and email domains. Microsoft login is enabled only when a client id, a client secret, and at least one tenant id are set |
| `ExternalLoginRelay` | `AllowedRedirectUris` (exact-match allow-list of client redirect URIs) and `AuthCodeTtlSeconds` |
| `MemberOfDomain` | Active Directory domain; empty selects the fake AD service |

The connection string is the framework default (`DefaultConnection`). Section placement per host: [Host § Configuration](Host.md#configuration).

## Design Notes

- **Persistence**: `IdentityDbContext` derives from the ASP.NET Core Identity context, not `BaseDbContext`, and uses the schema `identity`. It calls `Persistence`'s `AuditEntries`, `DispatchDomainEvents`, and `FixSqliteDateTimeOffset` directly. An asynchronous save runs in this order: audit → dispatch domain events → commit → publish the integration events buffered in the scoped, module-local `IntegrationEventCollector` through `IEventBus`. A failed commit clears the buffer. There is no outbox: a publish failure after the commit is logged and the events of that save are lost, which the per-event `Version` makes recoverable by a later event. A synchronous save with buffered integration events throws.
- **Publishing without a bus dependency**: the module publishes through `IEventBus` (vendor abstraction reached through `Shared`) and does not reference `EventBusMassTransitRabbitMQ`; the host registers the bus.
- **External-login relay**: the flow a separate-origin client uses is diagrammed in [README § Login Flow](../../README.md#login-flow-client--server). `ExternalLoginStart` accepts only the `Microsoft`/`EntraId` provider and an allow-listed `redirectUri`, then challenges Entra ID. `ExternalLoginRelay` resolves (links or provisions) the user, mints a token pair, stores it in the cache under a one-time code bound to the PKCE challenge (S256 of the verifier) with the configured TTL, and redirects to the client with `code` and `state`. `POST auth/token/external` consumes the code once with the verifier. The relay never sets an `Identity.Web` cookie. The relay pages and the exchange endpoint are rate-limited by the host's `external-login` policy — see [Host § Design Notes](Host.md#design-notes).
- **Two hosting modes for `Identity.Web`**: co-hosted in `Host` (JSON API and pages in one process — see [Host](Host.md)), or standalone via its own `Program` (cookie-only, Identity assembly only). The standalone host does not register JWT token services, so the relay completes end-to-end only under `Host`. The standalone composition calls `AddEventBus` itself (with no consumer assemblies), because `IdentityDbContext` requires `IEventBus`; nothing in that process consumes the events it publishes.
- **Schema creation and seed**: the module's migrations live in the migration projects, not in the module; `IdentityContextInitialiser` migrates and seeds roles and users — see [migrations.md § Migration sets](../conventions/migrations.md#migration-sets).

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Shared` (via `Identity.Contracts`) | project | Kernel types, `IntegrationEvent`, `ICurrentUser`/`IDateTime`, `IEventBus` abstraction |
| `Infrastructure` | project | `AppModule`, controller bases |
| `Persistence` | project | Configured DbContext registration, audit/dispatch extensions, provider support |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Identity.Core` | package | Identity store |
| `Lightsoft.ActiveDirectory`, `Lightsoft.Caching`, `Lightsoft.SharedKernel` | package | AD lookups, cache for the external-login codes, vendor kernel types |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` (`Identity.Web`) | package | Entra ID sign-in |

`Identity.Web` references `Identity` and `Infrastructure`. Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](dependency-graph.md).

## Depended On By

| Project | References |
|---|---|
| `Host` | `Identity`, `Identity.Web` |
| `Identity.Web` | `Identity` (intra-module) |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | `Identity` |
| `tests/Identity.Tests` | `Identity`, `Shared` |

No other module exists on this branch.

## Notable Conventions

- `IntegrationEventCollector` is module-local; it becomes a `Persistence` building block only once a second module needs it.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
