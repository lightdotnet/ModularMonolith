# Backend Solution Overview

ASP.NET Core (C#) Modular Monolith backend for the StarterKit template — one solution
(`StarterKit.slnx`). `src/StarterKit.WebApi` is the primary deployable process; `src/Identity.Web`
is also independently runnable as a login-only host (see § Entry Points). This file is the map;
layering and patterns are in [architecture.md](architecture.md), the project-reference graph in
[dependency-graph.md](dependency-graph.md), and each module's internals in
[modules/<Module>.md](modules/).

## Modules

| Module | Projects | Responsibility | Status |
|---|---|---|---|
| Identity | `src/Identity.Api` + `.Contracts` + `src/Identity.Web` | Users, roles, claims; API token issuance (password/AD), refresh, sessions; interactive cookie login + Microsoft Entra ID (OIDC) external login (`Identity.Web`); SignalR hub handshake token; permission catalog | Built, tested (100). Internal layering still informal — [known-debt.md](../known-debt.md) D1 |
| Notifications | `src/Notifications.Api` + `.Contracts` | Notification storage + real-time SignalR push; admin + self-service surfaces over one table; owns the welcome-mail handlers reacting to Identity's integration events | Built, no tests yet. No "mark all read", nothing sets `Archived` |
| Organization | `src/Organization.Api` + `.Contracts` | Companies, a self-referencing department/team hierarchy (`OrgUnit`), company-scoped employee levels, employees (membership history + optional Identity-login link) | Built, tested (63). Also exposes `IOrgDirectoryService`, a second cross-module seam consumed by LeaveManagement |
| Approval | `src/Approval.Api` + `.Contracts` | A generic, reusable multi-level approval engine — the caller resolves the approver chain and drives the workflow via `IApprovalService`; not tied to any request type | Built, tested (64) |
| LeaveManagement | `src/LeaveManagement.Api` + `.Contracts` | Self-service CRUD for employee leave requests; delegates the entire approval workflow to Approval via `IApprovalService`, resolves approvers/names via Organization's `IOrgDirectoryService` — no decide endpoint of its own | Built, tested (30) |
| Location | `src/Location.Api` + `.Contracts` | Self-referencing physical-location hierarchy (`Location`: Store/Warehouse/Terminal/Bin or any other data-driven type) plus a data-driven `LocationType` catalog (allowed-parent-type rules, not a hardcoded enum) | Built, tested (114). Also exposes `ILocationDirectoryService`, a cross-module seam with no current consumer. MSSQL-only migration set so far, not yet squashed to a baseline |
| Catalog | `src/Catalog.Api` + `.Contracts` | Self-referencing product-category tree (`Category`) plus `Product` CRUD/search/activate-deactivate/image management, priced via the shared `Money`/`VatPercentage` value objects | Built, tested (111). Also exposes `ICatalogPricingService`, a cross-module seam with no current consumer. MSSQL-only migration set so far (single unsquashed migration) |

## Shared / Host Projects

| Project | Responsibility |
|---|---|
| `src/Shared` | Shared kernel: entity/DTO wrappers over vendor `Light.Domain`, `ICurrentUser`/`IDateTime`, `PageQuery`/`SearchQuery`, permission-authorization building blocks (incl. `CurrentUserBase`'s `EmployeeId` claim accessor), mediator pipeline behaviors, constants. Leaf — no dependencies |
| `src/Infrastructure` | Cross-cutting infra: CORS, health checks, Serilog bootstrap, Mapster config, module/endpoint + API controller base classes, Basic Auth attribute. → `Shared`. EF Core concerns moved out to `Persistence` (2026-07) |
| `src/Persistence` | EF Core provider config, `BaseDbContext`, audit/soft-delete tracking + domain-event dispatch (meant to run inside each module's `SaveChangesAsync`), paging/result helpers, migration-time support, and an opt-in `Repositories/ICacheRepository<T>` whole-table cache-repository wrapper for small reference/lookup tables (zero adopters today — see [known-debt.md](../known-debt.md)). → `Shared` |
| `src/StarterKit.WebApi` | Composition-root host — the primary executable. Wires all seven modules, co-hosts `Identity.Web`'s login Razor Pages, and owns the API authentication composition (`Authentication/ApiAuthenticationExtensions`). → all seven modules + Identity.Web, Infrastructure, Shared |
| `src/Identity.Web` | Razor Pages login host inside the Identity module (cookie login + Microsoft OIDC). Co-hosted by `StarterKit.WebApi` and also runnable standalone (login-only). → `Identity.Api`, `Infrastructure` |

## Dependency Graph

One-way throughout: `Api`/`Contracts` → `Infrastructure`/`Persistence` → `Shared`;
`Identity.Web → Identity.Api` (intra-module); and `StarterKit.WebApi` → all seven business modules
plus `Identity.Web`. `Shared` is the only true leaf. Five compliant
business-module-to-business-module dependencies exist, each reaching only the target's `Contracts`
seam — `Location` and `Catalog` currently have none (no outgoing dependency, and no consumer of
either's own cross-module seam — `ILocationDirectoryService`/`ICatalogPricingService` — yet). The
full list and the project-reference diagram are in
[dependency-graph.md](dependency-graph.md). No circular references or boundary violations.

## Entry Points

- `src/StarterKit.WebApi/Program.cs` — builds a `WebApplication`, configures Serilog bootstrap,
  calls `ConfigureServices` / `AddLowercaseControllers` / `AddDefaultJsonOptions` /
  `AddInvalidModelStateHandler`, then `ConfigurePipelines()`, `UseWebSockets()`, and
  `MapEndpoints(config.GetValue<bool>("AllowAnonymous"))` before `Run()`.
- `src/Identity.Web/Program.cs` — a minimal login-only host: `AddIdentityWebHost(configuration)`,
  then a cookie-auth Razor Pages pipeline (`UseAuthentication`/`UseAuthorization`/`UseIdentityWeb`).
  No Bearer scheme, no `/api`, no SignalR hub.

## Data Access

One `DbContext` per module. `Identity`, `Notifications`, `Organization`, `Approval`,
`LeaveManagement`, `Location`, and `Catalog` share one physical database (each `DbConnectionNames.*`
aliases `Default`), separated by schema + table. Provider is configurable per environment
(`InMemory`/`PostgreSQL`/`MSSQL`/`Sqlite` via `IConfiguration["DbProvider"]`) — `Location` and
`Catalog` currently only have an MSSQL migration set (see
[modules/Location.md § Data Access](modules/Location.md#data-access) /
[modules/Catalog.md § Data Access](modules/Catalog.md#data-access)).

| Module | DbContext | Base | Detail |
|---|---|---|---|
| Identity | `IdentityDbContext` | ASP.NET Identity's `IdentityDbContext<...>` | [modules/Identity.md § Data Access](modules/Identity.md#data-access) |
| Notifications | `NotificationDbContext` | `Persistence` `BaseDbContext` | [modules/Notifications.md § Data Access](modules/Notifications.md#data-access) |
| Organization | `OrganizationDbContext` | `BaseDbContext` | [modules/Organization.md § Data Access](modules/Organization.md#data-access) |
| Approval | `ApprovalDbContext` | `BaseDbContext` | [modules/Approval.md § Data Access](modules/Approval.md#data-access) |
| LeaveManagement | `LeaveManagementDbContext` | `BaseDbContext` | [modules/LeaveManagement.md § Data Access](modules/LeaveManagement.md#data-access) |
| Location | `LocationDbContext` | `BaseDbContext` | [modules/Location.md § Data Access](modules/Location.md#data-access) |
| Catalog | `CatalogDbContext` | `BaseDbContext` | [modules/Catalog.md § Data Access](modules/Catalog.md#data-access) |

## External Dependencies

- **`Lightsoft.*` (namespace `Light.*`)** — private vendor family: `.Mediator`/`.Contracts`, `.Result`,
  `.SharedKernel`, `.AspNetCore.Authorization`, `.AspNetCore.Modularity`, `.AspNetCore.Extensions`,
  `.AspNetCore.Swagger`, `.EntityFrameworkCore`, `.Caching`, `.Serilog`, `.SmtpMail`,
  `.ActiveDirectory` (Identity only). Suspected-dead: `.EventBus`, `.FileGenerator` — see
  [known-debt.md](../known-debt.md).
- **EF Core providers** (`Persistence`) — InMemory / Sqlite / SqlServer / Npgsql.
- **`Microsoft.AspNetCore.Identity.EntityFrameworkCore`**, **`Microsoft.Extensions.Identity.Core`**
  (`Identity.Api`) — ASP.NET Identity base types.
- **`Microsoft.AspNetCore.Authentication.OpenIdConnect`** (`Identity.Web`) — Microsoft Entra ID
  external-login scheme.
- **`Microsoft.AspNetCore.Authentication.JwtBearer`** (`StarterKit.WebApi`) — the host-owned Bearer
  and `"HubBearer"` schemes.
- **`FluentValidation`** (`Shared`) — backs `ValidationBehaviour`; `Location` is the first module with
  actual registered validators (Contracts DTO + thin command validators) — see
  [conventions/coding-conventions.md](../conventions/coding-conventions.md).
- **`Mapster`** (`Shared`) — object mapping, configured in `Infrastructure/Mappings/MapsterSettings.cs`.
- **`AspNetCore.HealthChecks.UI.Client`**, **`Spectre.Console`** (startup banner) — host.
- **`Microsoft.AspNetCore.SignalR`** (`Notifications.Api`) — shared-framework reference.

## Client Integration

`clients/admin/` (a Next.js admin dashboard) consumes the business modules over HTTP — `Identity`,
`Notifications` (REST + a browser-direct WebSocket to `/signalr-hub` authenticated with a short-lived
hub token), `Organization`, `Approval`, and `LeaveManagement`; `Location` and `Catalog` have no
client consumer yet.
See [../../../clients/admin/docs/architecture/overview.md](../../../clients/admin/docs/architecture/overview.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-12_
