# Architecture: Backend

Layering, dependency direction, and cross-cutting patterns. Per-module internals, public contracts,
and module-specific debt live in each module doc under [modules/](modules/); the project-reference
graph and the cross-module dependency inventory live in [dependency-graph.md](dependency-graph.md).

## Layering

Module structure convention (adopted 2026-07-30): every module is one or more flat projects directly
under `src/` (no `src/Modules/` nesting) — either a single `<Module>` / `<Module>.Api` project
organized by folder, or, if complex enough, split Clean-Architecture-style into
`<Module>.Domain`/`.Application`/`.Infrastructure`/`.Api`. Every module also gets a
`<Module>.Contracts` seam — the only project other modules or the host may reference.

Five modules exist. Four are a single `.Api` project plus `.Contracts`; **Identity** additionally has
`Identity.Web` (a Razor Pages login host inside the same bounded context — see its module doc). The
`.Api` suffix is a deliberate future-extraction candidate. Structural summary only — full layering per
module doc:

| Module | Structure | CQRS shape | Tests |
|---|---|---|---|
| Identity | `Identity.Api` + `Identity.Contracts` + `Identity.Web` | Handlers delegate to `UserService`/`RoleService` — a half-migration ([known-debt.md](../known-debt.md) D1) | `tests/Identity.Tests`, 100 |
| Notifications | `Notifications.Api` + `.Contracts` | Same half-migration as Identity; controllers split by **audience** (admin vs. self-service) not resource | none yet |
| Organization | `Organization.Api` + `.Contracts` (seam split into per-feature subfolders) | Handlers own their `OrganizationDbContext` logic directly — no service layer | `tests/Organization.Tests`, 63 |
| Approval | `Approval.Api` + `.Contracts` | Workflow rules live on the `ApprovalRequest` aggregate (`Create`/`Decide`/`Cancel` return `IResult`); `IApprovalService` is a thin coordinator over it (must be DI-reachable cross-module); read-path handlers own their logic directly | `tests/Approval.Tests`, 60 |
| LeaveManagement | `LeaveManagement.Api` + `.Contracts` | Handlers own their logic directly (Organization's shape); command handlers inject `IApprovalService` + `IOrgDirectoryService` straight into constructors, the leave-request read queries touch neither | `tests/LeaveManagement.Tests`, 30 |

Below the module layer: `src/Shared` (leaf) and `src/Persistence` (→ `Shared`) are the pre-module
shared kernel; `src/Infrastructure` (→ `Shared`) is cross-cutting infra, no longer holding EF Core
concerns (moved to `Persistence` in the 2026-07 refactor). `Persistence`'s
`QueryableResultExtensions.ToPagedAsync` clamps `pageSize` to 100 — affects every paginated endpoint
across all five modules.

## Hosts

`StarterKit.WebApi` is the primary, full deployable process — it wires all five modules via
`app.MapEndpoints(...)`, maps the `Notifications` SignalR hub at `/signalr-hub`, co-hosts the
`Identity.Web` Razor Pages login (`AddIdentityWeb` + `UseIdentityWeb`), and owns the API
authentication composition (`Authentication/ApiAuthenticationExtensions.AddApiAuthentication`).

`Identity.Web` is also **independently runnable** as a login-only standalone host
(`src/Identity.Web/Program.cs`): cookie-authenticated Razor Pages only, no Bearer/policy/hub schemes,
no `/api`. It composes the Identity assembly's platform + mediator services itself and scans only that
assembly, so it has no cross-module integration-event handlers ([known-debt.md](../known-debt.md) D6).
The two hosts hand-compose the Identity platform independently and have drifted
([known-debt.md](../known-debt.md), structural).

## Dependency Direction

Expected `Api → Application → Domain`; not compiler-enforceable within a module (folder-based
discipline) but it holds informally — `Controllers/` call only services/`Mediator`, never
`Entities`/`Data`/DbContext directly.

The "no module references another module's internals" rule is **verified across all five modules** —
every cross-module dependency reaches only the target's `Contracts` seam, with no reference the other
direction and no cycle. `Identity.Web → Identity.Api` is a direct internal reference but stays inside
the Identity bounded context, so it is not a cross-module edge. The full list of the five compliant
cross-module edges is in [dependency-graph.md § Cross-Module Boundary Violations](dependency-graph.md#cross-module-boundary-violations-backend-only);
the project-reference diagram is in [§ Circular References](dependency-graph.md#circular-references).

Note the Identity↔Notifications edge flipped this session: `Identity.Api` no longer references
`Notifications.Contracts`; instead `Notifications.Api → Identity.Contracts` (welcome-mail handlers
reacting to Identity's integration events).

## Key Design Patterns

- **Mediator** via vendor `Light.Mediator`, with two pipeline behaviors registered in
  `StarterKit.WebApi/ConfigureExtensions.cs` (outermost first): `LoggingBehaviour` then
  `ValidationBehaviour` (FluentValidation). The standalone `Identity.Web` host registers the same two
  behaviors over the Identity assembly only.
- **Result pattern** via vendor `Light.Contracts.Result`/`Result<T>`/`PagedResult<T>` instead of
  throwing for expected failures. Aggregate methods on `ApprovalRequest` (`Create`/`Decide`/`Cancel`)
  also return `IResult`/`IResult<T>` rather than throwing on a broken invariant.
- **Cross-module reactions via `Contracts`-level integration events** (`INotification`), published
  through `IPublisher`, handled by an `INotificationHandler<T>` in another module's assembly (one
  mediator spans every module assembly):
  - `Identity` — `UserCreatedIntegrationEvent` from `CreateUserCommandHandler`,
    `ExternalUserProvisionedIntegrationEvent` from `ExternalLoginService`, both hand-published and
    handled in `Notifications.Api`. Identity defines no `BaseEntity` domain events and does not wire
    the `DispatchDomainEventsExtensions` convention — [known-debt.md](../known-debt.md) P6.
  - `Approval` — `ApprovalFinalizedIntegrationEvent`, published by `ApprovalService` after a decide
    commits to a terminal `Approved`/`Rejected` state (a requester-initiated cancel is driven by the
    owning module and is not re-published), and handled in-process by `LeaveManagement.Api`. Approval
    also raises in-module `BaseEntity` domain events and dispatches them via
    `publisher.DispatchDomainEvents(this)` from `ApprovalDbContext.SaveChangesAsync` (wrapped in
    try/catch-log) — the conforming reference for the convention P6 tracks as still-missing in
    Identity.
  - `LeaveManagement` — subscribes to `ApprovalFinalizedIntegrationEvent` to reconcile the local
    `LeaveRequest.Status`, with `LeaveRequestReconciliationService` (a `BackgroundService`, the only
    one in the backend) as the periodic delivery backstop. `Organization` uses no events.
  The repo-wide `DispatchDomainEventsExtensions` convention (`src/Persistence`, meant to run inside a
  module's `SaveChangesAsync`) is wired only in `Approval`; the `Identity` gap is
  [known-debt.md](../known-debt.md) P6.
- **Authentication composition is host-owned, two-layer.** `Identity.Api` registers only token
  *services* (`AddJwtTokenServices` — signing, token/hub-token issuers, session + auth services), no
  scheme. `Identity.Web` registers the cookie + Microsoft OIDC schemes. The co-host
  (`AddApiAuthentication`) adds the JWT `"Bearer"` scheme, the `"Identity.CookieOrBearer"` policy
  scheme (Bearer header → `"Bearer"`; hub path + `?access_token=` → fail-closed `"HubBearer"`; else
  cookie), a `"Bearer"` `PostConfigure` rejecting the hub audience and flowing token expiry into the
  ticket, and the `HubTokenApiGuardHandler` backstop. See [modules/Identity.md § Authentication Wiring](modules/Identity.md#authentication-wiring).
- **SignalR hub handshake token.** The browser never receives the full session JWT for the hub
  WebSocket. `POST api/v1/auth/token/hub` mints a ~120s token scoped to `aud = "signalr-hub"`
  carrying only `uid` + `jti`; the hub authenticates it on the `"HubBearer"` sub-scheme, and
  `CloseOnAuthenticationExpiration` drops the connection when it lapses. See
  [modules/Notifications.md](modules/Notifications.md) and [docs/integration.md](../../../docs/integration.md).
- **Extension-method DI registration** — each feature area exposes `Add<Feature>`/`Use<Feature>`.
- **CQRS at the controller boundary, three shapes.** Every controller action binds a `Contracts` DTO
  and dispatches an `internal` mediator command/query. `Identity`/`Notifications` forward to a service
  class ([known-debt.md](../known-debt.md) D1); `Organization`/`LeaveManagement` hold the DbContext
  logic directly; `Approval` splits by audience, with the write-path workflow rules on the
  `ApprovalRequest` aggregate behind a thin `IApprovalService`. See each module doc.
- **Audience-split controllers + real-time push** in `Notifications` — an admin controller (explicit
  permissions) and a self-service controller (permission-less, hard-scoped via `ICurrentUser`) over
  one table, plus a push-only SignalR hub at `/signalr-hub`.

## Shared Kernel / Common Building Blocks

- **`src/Shared`** (leaf) — `ICurrentUser`/`IDateTime`, `CurrentUserBase` (claims-driven, incl. an
  `EmployeeId` accessor), `Status` value object, `PageQuery`/`IPage`/`SearchQuery`,
  `BaseDto`/`BaseDto<TId>`, `AffectedRowsResult`, entity wrappers, the mediator pipeline behaviors,
  permission authorization (`SuperUserPolicy`, `AccessControl`, internal
  `PolicyProvider`/`AuthorizationHandler`), constants (`ClaimTypeConstants` incl.
  `EmployeeId = "employee_id"`, `CronTimeConstants`), `ReflectionHelper`.
- **`src/Infrastructure`** (→ `Shared`) — CORS, health checks, Mapster config, module/endpoint base
  classes (`AppModule`, `AppModuleEndpoint`), API controller base classes + `BasicAuthAttribute`,
  static Serilog bootstrap (`AppLogging`).
- **`src/Persistence`** (→ `Shared`) — EF Core provider wiring (`DbContextExtensions`, `DbProvider`,
  `DbConnectionNames` — all aliasing `Default`), `BaseDbContext`,
  `TrackingExtensions`/`DispatchDomainEventsExtensions`, paging/result helpers, migration-time runtime
  support.
- **`<Module>.Contracts`** — the per-module seam. None is a true leaf (every one references `Shared`);
  `Shared` is the only true leaf.

## Module/Route Boundaries

`StarterKit.WebApi` wires all five modules via `app.MapEndpoints(...)`, maps the `Notifications`
SignalR hub at `/signalr-hub`, and serves the `Identity.Web` login Razor Pages under `/Account/*`.
The full route/permission inventory per module lives in each module doc's § Public Contract.

## Known Architectural Risks / Debt

Module-specific debt lives in each module doc's § Notable Conventions and the canonical
[known-debt.md](../known-debt.md). Only genuinely cross-cutting risks not owned by a single module
are tracked here:

| Finding | Severity | Notes |
|---|---|---|
| `Persistence/MigrationSupport/MigrationsExtensions.AddMigrationsServices` registers mediator handlers via `Assembly.GetExecutingAssembly()` (the `Persistence` assembly) | Medium | Won't pick up handlers in module assemblies — revisit once a module with domain-event handlers relies on migration-time dispatch. |
| `ApiControllerBase`/`VersionedApiController` (`src/Infrastructure/Endpoints/`) duplicate an identical `_mediator` lazy property | Low | Likely unavoidable — they derive from two different vendor base classes. |
| `Identity.Web` and `StarterKit.WebApi` hand-compose the Identity platform + mediator independently and have drifted | Low–Medium | See [known-debt.md](../known-debt.md) (structural) — candidate fix is a shared `AddIdentityHost` composition helper. |

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-09_
