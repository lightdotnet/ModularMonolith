# Project Overview: Identity

The Identity module is three projects forming one bounded context:

| Project | Assembly | Role |
|---|---|---|
| `src/Identity.Contracts` | `StarterKit.Modules.Identity.Contracts` | The cross-module seam — the only Identity project another module may reference |
| `src/Identity` | `StarterKit.Modules.Identity` | The module implementation |
| `src/Identity.Web` | `StarterKit.Modules.Identity.Web` | Razor Pages for cookie login, the Microsoft external-login relay, and the admin pages for users, roles, and permissions |

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
| `IntegrationEvents/*` | Integration events (`IntegrationEvent` records) published when a user is provisioned, updated, has its status changed, or is deleted. Each carries the user's full current state and a `Version`; consumers must be idempotent on the user id and drop events that are not newer than the one already applied. `UserProvisionedIntegrationEvent` carries a `ProvisioningSource` |

**`Identity`**:

| Area | Role |
|---|---|
| `IdentityModule` | The module's `AppModule`: registers the Identity services, JWT token services, and the permission provider |
| `DependencyInjection.AddIdentityServices` | Identity store over `IdentityDbContext` (with the module's claims-principal factory), Active Directory (a fake service unless `MemberOfDomain` is set and the OS is Windows), user/role/external-login services, the external-login code store, the integration-event collector, and `IIdentityModuleApi` |
| `Endpoints/` | Controllers on `VersionedApiController`. `TokenController` (`api/v{version}/auth`) exposes `token/get`, `token/refresh`, `token/external` (anonymous) and `token/hub` (authenticated); the other controllers manage users, roles, permissions, and the current user's profile and sessions |

Visibility inside `Identity`: the domain entities, request/response models, options and result types, and service interfaces are `public`; their implementations, `IdentityDbContext` and its initialiser, the token services, the `IIdentityModuleApi` implementation, the permission catalog, the CQRS commands/handlers, and the integration-event buffering are `internal`. `InternalsVisibleTo` grants `Identity.Tests`, `Identity.Web`, and the three migration projects.

**`Identity.Web`**:

| Type | Role |
|---|---|
| `DependencyInjection.AddIdentityWeb` / `UseIdentityWeb` | Application and external cookies (the application cookie revalidates against the user's security stamp), the `SignInManager`, the optional `Microsoft` OIDC scheme, authorization, the admin options and route convention, and Razor Pages |
| `Authentication/IdentitySignInManager` | The registered `SignInManager<User>`: refuses sign-in for a user who is not active or is soft-deleted, and rejects an existing cookie of such a user on security-stamp revalidation |
| `Pages/Account/*` | Interactive login/logout and Microsoft login, plus the relay pair `ExternalLoginStart` / `ExternalLoginRelay` for separate-origin clients |
| `Pages/Admin/*` | Admin pages for users, roles, and the role × permission matrix. Each page is gated by the Identity permission policies. Each section can be switched off by configuration, which removes its routes (`Admin/IdentityAdminOptions`, `Admin/AdminPagesConvention`) |
| `TagHelpers/` | Generic Bootstrap UI tag helpers with no Identity types: permission and feature gating, form field, search form, pager, status badge, confirm-post, flash messages, nav link |

## Configuration

| Section | Purpose |
|---|---|
| `Jwt` | Issuer, signing key, token lifetimes, hub audience |
| `Authentication:Microsoft` | Entra ID client id/secret, instance, callback path, allow-listed tenant ids and email domains. Microsoft login is enabled only when a client id, a client secret, and at least one tenant id are set |
| `ExternalLoginRelay` | `AllowedRedirectUris` (exact-match allow-list of client redirect URIs) and `AuthCodeTtlSeconds` |
| `IdentityWeb:Admin` | `Enabled` (master switch for `/Admin`), `Users`, `Roles`, `Permissions` — each defaults to `true`; a disabled section has no route and its links are hidden |
| `IdentityWeb:SecurityStampValidationInterval` | `TimeSpan` interval at which the application cookie is revalidated against the security stamp; defaults to 5 minutes, a non-positive value fails startup |
| `MemberOfDomain` | Active Directory domain; empty selects the fake AD service |

The connection string is the framework default (`DefaultConnection`). Section placement per host: [StarterKit.WebApi § Configuration](WebApi.md#configuration).

## Design Notes

- **Persistence**: `IdentityDbContext` derives from the ASP.NET Core Identity context, not `BaseDbContext`, and uses the schema `identity`. It calls `Persistence`'s `AuditEntries`, `DispatchDomainEvents`, and `FixSqliteDateTimeOffset` directly. An asynchronous save runs in this order: audit → dispatch domain events → commit → publish the integration events buffered in the scoped, module-local `IntegrationEventCollector` through `IEventBus`. A failed commit clears the buffer. There is no outbox: a publish failure after the commit is logged and the events of that save are lost, which the per-event `Version` makes recoverable by a later event. A synchronous save with buffered integration events throws.
- **Publishing without a bus dependency**: the module publishes through `IEventBus` (vendor abstraction reached through `Shared`) and does not reference `EventBusMassTransitRabbitMQ`; the host registers the bus. The Notifications module consumes `UserProvisionedIntegrationEvent` — see [Notifications § Design Notes](Notifications.md#design-notes).
- **Cookie principal**: `IdentityClaimsPrincipalFactory` (`Authentication/`) adds the platform user-id and user-name claims to the cookie principal, so `ICurrentUser`, auditing, and super-user checks work for cookie sessions as they do for a self-issued JWT; role and permission claims come from the base factory.
- **Session revocation**: `UserService` rotates a user's security stamp when the user's roles, claims, or active status change, and `RoleService` rotates the stamp of every member when a role's claims change. The application cookie revalidates on the configured interval, so an affected session is dropped at its next revalidation. JWT and refresh tokens are not stamp-checked; `AuthenticationService` applies its own active/deleted check instead.
- **External-login relay**: the flow a separate-origin client uses is diagrammed in [README § Login Flow](../../../README.md#login-flow-client--server). `ExternalLoginStart` accepts only the `Microsoft`/`EntraId` provider and an allow-listed `redirectUri`, then challenges Entra ID. `ExternalLoginRelay` resolves (links or provisions) the user, mints a token pair, stores it in the cache under a one-time code bound to the PKCE challenge (S256 of the verifier) with the configured TTL, and redirects to the client with `code` and `state`. `POST auth/token/external` consumes the code once with the verifier. The relay never sets an `Identity.Web` cookie. The relay pages and the exchange endpoint are rate-limited by the host's `external-login` policy — see [StarterKit.WebApi § Design Notes](WebApi.md#design-notes).
- **Two hosting modes for `Identity.Web`**: co-hosted in `StarterKit.WebApi` (JSON API and pages in one process — see [StarterKit.WebApi](WebApi.md)), or standalone via its own `Program` (cookie authentication only, Identity assembly only). The standalone composition (`IdentityWebHost`) registers the permission policies, permission authorization, and the Identity permission provider, so the admin pages are gated the same way in both modes. It does not register JWT token services, so the relay completes end-to-end only under `StarterKit.WebApi`. It calls `AddEventBus` itself (with no consumer assemblies), because `IdentityDbContext` requires `IEventBus`; nothing in that process consumes the events it publishes.
- **Schema creation and seed**: the module's migrations live in the migration projects, not in the module; `IdentityContextInitialiser` migrates and seeds roles and users — see [migrations.md § Migration sets](../../conventions/migrations.md#migration-sets).

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Shared` (via `Identity.Contracts`) | project | Kernel types, `IntegrationEvent`, `ICurrentUser`/`IDateTime`, `IEventBus` abstraction |
| `Infrastructure` | project | `AppModule`, controller bases |
| `Persistence` | project | Configured DbContext registration, audit/dispatch extensions, provider support |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Identity.Core` | package | Identity store |
| `Lightsoft.ActiveDirectory`, `Lightsoft.Caching`, `Lightsoft.SharedKernel` | package | AD lookups, cache for the external-login codes, vendor kernel types |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` (`Identity.Web`) | package | Entra ID sign-in |

`Identity.Web` references `Identity`, `Infrastructure`, and `EventBusMassTransitRabbitMQ`. Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](../dependency-graph.md).

## Depended On By

| Project | References |
|---|---|
| `StarterKit.WebApi` | `Identity`, `Identity.Web` |
| `Identity.Web` | `Identity` (intra-module) |
| `Notifications` | `Identity.Contracts` — the integration event it consumes; see [Notifications](Notifications.md) |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | `Identity` |
| `tests/Identity.Tests` | `Identity`, `Identity.Web`, `Shared` |

## Notable Conventions

- `IntegrationEventCollector` is module-local; it becomes a `Persistence` building block only once a second module needs it.
- The `Identity.Web` tag helpers stay in the module until a second module's `.Web` project needs them; they are then candidates for a framework project.
- The handlers forward to services and `IIdentityModuleApi` reads the context and calls services directly — a documented deviation, see [coding-conventions.md § Deviations](../../conventions/coding-conventions.md#deviations-from-norms-elsewhere-in-the-repo).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
