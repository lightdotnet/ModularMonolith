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

Twelve modules exist. Eleven are a single `.Api` project plus `.Contracts`; **Identity** additionally has
`Identity.Web` (a Razor Pages login host inside the same bounded context — see its module doc). The
`.Api` suffix is a deliberate future-extraction candidate. Structural summary only — full layering per
module doc:

| Module | Structure | CQRS shape | Tests |
|---|---|---|---|
| Identity | `Identity.Api` + `Identity.Contracts` + `Identity.Web` | Handlers delegate to `UserService`/`RoleService` — a half-migration ([known-debt.md](../known-debt.md) D1) | `tests/Identity.Tests` |
| Notifications | `Notifications.Api` + `.Contracts` | Same half-migration as Identity; controllers split by **audience** (admin vs. self-service) not resource | none yet |
| Organization | `Organization.Api` + `.Contracts` (seam split into per-feature subfolders) | Handlers own their `OrganizationDbContext` logic directly — no service layer | `tests/Organization.Tests` |
| Approval | `Approval.Api` + `.Contracts` | Workflow rules live on the `ApprovalRequest` aggregate (`Create`/`Decide`/`Cancel` throw `Light.Exceptions.*` on a broken invariant); `IApprovalService` is a thin coordinator over it — maps those exceptions back to `IResult` (must be DI-reachable cross-module); read-path handlers own their logic directly | `tests/Approval.Tests` |
| LeaveManagement | `LeaveManagement.Api` + `.Contracts` | Workflow rules live on the `LeaveRequest` aggregate (`Create`/`LinkApprovalRequest`/`Resubmit`/`ReviseDetails`/`ApplyApprovalOutcome`, same throw-then-translate shape as `Approval`); `LeaveRequestApprovalCoordinator` is the shared seam for cross-row/cross-module orchestration (Approval reconcile, overlap check, approver resolution) the command handlers and the reconciliation sweep all call into; the leave-request read queries touch neither | `tests/LeaveManagement.Tests` |
| Location | `Location.Api` + `.Contracts` | Handlers own their `LocationDbContext` logic directly, same as `Organization`/`LeaveManagement`; the allowed-parent-type invariant lives on the `Location`/`LocationType` aggregates, input-shape validation is FluentValidation's job (see § Key Design Patterns) | `tests/Location.Tests` |
| Catalog | `Catalog.Api` + `.Contracts` | Handlers own their `CatalogDbContext` logic directly, same as `Organization`/`LeaveManagement`/`Location`; `Category`/`Product` guard state transitions via behaviour methods but carry no invariant heavier than Location's (no type discriminator) | `tests/Catalog.Tests` |
| Currency | `Currency.Api` + `.Contracts` | Handlers own their `CurrencyDbContext` logic directly; the base-currency and rate-ordering invariants live on the `Currency`/`ExchangeRate` entities, and the exchange-rate history is append-only (see § Key Design Patterns) | `tests/Currency.Tests` |
| Orders | `Orders.Api` + `.Contracts` | Handlers own their `OrdersDbContext` logic directly, same as `Organization`/`LeaveManagement`/`Location`/`Catalog`; workflow rules (draft-only editing, `Place`/`Cancel`/`MarkFulfilled` state transitions) live on the `Order` aggregate, with `Payment` as a separate aggregate root sharing the same `DbContext` | `tests/Orders.Tests` |
| Inventory | `Inventory.Api` + `.Contracts` | Handlers own their `InventoryDbContext` logic directly, same as the modules above; the no-oversell invariant lives on the `StockLevel` aggregate (`Apply` throws), with the internal `StockLedger` service coordinating `StockLevel`, the immutable `StockAdjustment` ledger, and moving-average pricing in one commit | `tests/Inventory.Tests` |
| Transfers | `Transfers.Api` + `.Contracts` | Handlers own their `TransfersDbContext` logic directly; the in-transit state machine and quantity rules live on the `StockTransfer` aggregate, with a shared `TransferPosting` class holding the Inventory-posting second half of dispatch/receive | `tests/Transfers.Tests` |
| Purchasing | `Purchasing.Api` + `.Contracts` | Handlers own their `PurchasingDbContext` logic directly; state and quantity rules live on the `PurchaseOrder`/`GoodsReceipt`/`PurchaseReturn` aggregates, `PurchaseOrderApprovalCoordinator` is the shared Approval seam (same role as LeaveManagement's), and a shared `PurchasingPosting` class holds the Inventory-posting second half of receipts/returns | `tests/Purchasing.Tests` |

Below the module layer: `src/Shared` (leaf) and `src/Persistence` (→ `Shared`) are the pre-module
shared kernel; `src/Infrastructure` (→ `Shared`) is cross-cutting infra, no longer holding EF Core
concerns (moved to `Persistence` in the 2026-07 refactor). `Persistence`'s
`QueryableResultExtensions.ToPagedAsync` clamps `pageSize` to 100 — affects every paginated endpoint
across all modules that page.

## Hosts

`StarterKit.WebApi` is the primary, full deployable process — it wires all twelve modules via
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

The "no module references another module's internals" rule is **verified across every module** —
every cross-module dependency reaches only the target's `Contracts` seam, with no reference the other
direction and no cycle. `Identity.Web → Identity.Api` is a direct internal reference but stays inside
the Identity bounded context, so it is not a cross-module edge. The full list of the nineteen compliant
cross-module edges is in [dependency-graph.md § Cross-Module Boundary Violations](dependency-graph.md#cross-module-boundary-violations-backend-only);
the project-reference diagram is in [§ Circular References](dependency-graph.md#circular-references).
The stock-owning seam is `Inventory`'s `IInventoryService`, consumed synchronously and in-process (not via
an integration event, see § Key Design Patterns) by `Orders`, `Transfers`, and `Purchasing`; `Inventory`
itself consumes only `Location`'s `ILocationDirectoryService` and has no outgoing dependency on any of
them. `Location`'s `ILocationDirectoryService` is consumed by `Orders`, `Inventory`, `Transfers`, and
`Purchasing`; `Catalog`'s `ICatalogPricingService` by `Orders`, `Transfers`, and `Purchasing`;
`Currency`'s `ICurrencyService` by `Orders` and `Catalog`;
`Organization`'s `IOrgDirectoryService` by `LeaveManagement` and `Purchasing`; `Approval`'s
`IApprovalService` and integration event by `LeaveManagement` and `Purchasing`. `Location` and `Currency`
have no outgoing cross-module dependency of their own. `Identity.Api` references no other module;
`Notifications.Api → Identity.Contracts` (welcome-mail handlers reacting to Identity's integration events).

## Key Design Patterns

- **Mediator** via vendor `Light.Mediator`, with two pipeline behaviors registered in
  `StarterKit.WebApi/ConfigureExtensions.cs` (outermost first): `LoggingBehaviour` then
  `ValidationBehaviour` (FluentValidation). The standalone `Identity.Web` host registers the same two
  behaviors over the Identity assembly only.
- **Result pattern** via vendor `Light.Contracts.Result`/`Result<T>`/`PagedResult<T>` instead of
  throwing for expected failures — the default across command handlers and services. The
  `ApprovalRequest` aggregate is the deliberate exception: `Create`/`Decide`/`Cancel` throw typed
  `Light.Exceptions.*` on a broken invariant, and `ApprovalService` maps those back to a `Result`
  at the `IApprovalService` seam.
- **Cross-module reactions via `Contracts`-level integration events** (`INotification`), published
  through `IPublisher`, handled by an `INotificationHandler<T>` in another module's assembly (one
  mediator spans every module assembly):
  - `Identity` — `UserCreatedIntegrationEvent` from `CreateUserCommandHandler`,
    `ExternalUserProvisionedIntegrationEvent` from `ExternalLoginService`, both hand-published and
    handled in `Notifications.Api`. Identity defines no `BaseEntity` domain events and does not wire
    the `DispatchDomainEventsExtensions` convention — [known-debt.md](../known-debt.md) P6.
  - `Approval` — `ApprovalFinalizedIntegrationEvent`, published by `ApprovalService` after a decide
    commits to a terminal `Approved`/`Rejected` state (a requester-initiated cancel is driven by the
    owning module and is not re-published), and handled in-process by `LeaveManagement.Api` and
    `Purchasing.Api`. Approval also raises in-module `BaseEntity` domain events and dispatches them via
    `publisher.DispatchDomainEvents(this)` from `ApprovalDbContext.SaveChangesAsync` (wrapped in
    try/catch-log) — the conforming reference for the convention P6 tracks as still-missing in
    Identity. Request types owned by another module are reserved in
    `Approval.Contracts/Approvals/ApprovalRequestTypes`: the HTTP create endpoints reject them, only the
    owning module creates its own through `IApprovalService.CreateAsync`, and the owner reconciles by the
    approval request id it stored (`GetStatusAsync`/`GetStatusesAsync`) — see
    [modules/Approval.md](modules/Approval.md).
  - `LeaveManagement` and `Purchasing` — subscribe to `ApprovalFinalizedIntegrationEvent` to reconcile
    the local status (`LeaveRequest.Status` / `PurchaseOrder.Status`), each with its own reconciliation
    `BackgroundService` as the periodic delivery backstop. `Organization`/`Location`/`Catalog`/`Currency`
    use no events.
  - `Orders` — `OrderPlacedIntegrationEvent`, published by `PlaceOrderCommandHandler` directly after
    `SaveChangesAsync` commits (best-effort, logged-not-thrown), for any future consumer; no handler
    exists yet. `Inventory` deliberately does **not** subscribe to it — stock movement instead goes
    through the synchronous seam described below, because the strict no-oversell requirement needs the
    decrement to reject an oversell before `Orders` itself commits, a guarantee a best-effort async
    event cannot give. `Order` also raises in-module `BaseEntity` domain events
    (`OrderPlacedEvent`/`OrderCancelledEvent`/`OrderFulfilledEvent`) dispatched the same
    `publisher.DispatchDomainEvents(this)` way from `OrdersDbContext.SaveChangesAsync`, but none has a
    subscriber yet either — see [modules/Orders.md](modules/Orders.md). `Transfers` and `Purchasing`
    dispatch their aggregates' domain events the same way, also with no subscriber.
  The repo-wide `DispatchDomainEventsExtensions` convention (`src/Persistence`, meant to run inside a
  module's `SaveChangesAsync`) is wired in `Approval`, `Orders`, `Transfers`, and `Purchasing`; the
  `Identity` gap is [known-debt.md](../known-debt.md) P6.
- **Synchronous, DI-only cross-module seam as a second integration mechanism, distinct from the
  event-based pattern above.** `Inventory.Contracts`'s `IInventoryService` is called directly and
  synchronously, in the same request, rather than through a published `INotification`. `Orders`'
  `PlaceOrderCommandHandler` calls `DecrementForOrderAsync` *before* its own `SaveChangesAsync`, so an
  oversell prevents the order from ever being persisted, and a subsequent `Orders` save failure triggers a
  best-effort compensating `RestoreForOrderAsync`; `CancelOrderCommandHandler` commits first, then
  best-effort restores. `Transfers` (dispatch/receive) and `Purchasing` (goods receipt/purchase return) use
  `IssueStockAsync`/`ReceiveStockAsync`. This trades the event-based pattern's full decoupling for a
  synchronous correctness guarantee (an all-or-nothing check). There is no shared transaction, so every
  call is idempotent per source line (an idempotency key on the ledger) and each consuming module owns a
  reconciliation sweep for the window between Inventory's commit and its own — see
  [modules/Inventory.md § Notable Conventions](modules/Inventory.md#notable-conventions).
- **Multi-currency: one base currency, converted on entry.** `Currency` owns the currency list and an
  append-only rate history; `ICurrencyService` resolves the base currency and the rate effective at an
  instant (a missing rate is an error, never a silent 1:1). `Orders` stores every order in the base
  currency and converts a foreign-priced product once, when a line is added, rounding with
  `CurrencyRounding.RoundToMinorUnits`; `Catalog` only checks that a product's price currency is active.
  `Inventory`, `Transfers`, and `Purchasing` do not consume `Currency`. See
  [modules/Currency.md](modules/Currency.md) and [modules/Orders.md](modules/Orders.md).
- **Append-only records: a correction is a new record.** `Inventory`'s `StockAdjustment` ledger,
  `Currency`'s `ExchangeRate` history (`CurrencyDbContext` refuses to save a modified or deleted rate as a
  backstop), and `Purchasing`'s `GoodsReceipt` have no edit or delete path.
- **Posting-first two-commit documents** (`Transfers`, `Purchasing`): a document that needs an id to post
  stock under is committed in a `Posting` state first, Inventory is called, then a second commit records
  the outcome; a deterministic Inventory refusal with nothing landed aborts/voids the document, anything
  that landed is rolled forward, and nothing posted is ever reversed. A shared `*Posting` class serves both
  the command handlers and the sweep. See [modules/Transfers.md](modules/Transfers.md) /
  [modules/Purchasing.md](modules/Purchasing.md).
- **Module-owned reconciliation `BackgroundService`s** are the backstop for every cross-module
  best-effort step: `LeaveManagement` and `Purchasing` reconcile pending approvals against `Approval`;
  `Orders` restores stock for never-placed orders still holding a decrement; `Transfers` and `Purchasing`
  finish documents stuck in `Posting`. Each binds options from its own configuration section (validated on
  start, every default in code), creates a fresh DI scope per tick, and pages candidates in bounded batches.
- **Provider-aware filtered indexes.** A filtered index is declared with `HasProviderFilter`
  (`src/Persistence/Extensions/IndexBuilderExtensions.cs`), which applies the bracket-quoted predicate on
  every provider except Npgsql and a double-quoted one on Npgsql; every module with a filtered index uses
  it. See [../conventions/migrations.md](../conventions/migrations.md).
- **Authentication composition is host-owned, two-layer.** `Identity.Api` registers only token
  *services* (`AddJwtTokenServices` — signing, token/hub-token issuers, session + auth services), no
  scheme. `Identity.Web` registers the cookie + Microsoft OIDC schemes. The co-host
  (`AddApiAuthentication`) adds the JWT `"Bearer"` scheme, the `"Identity.CookieOrBearer"` policy
  scheme (routes by request path: hub path → fail-closed `"HubBearer"`; `/api` → `"Bearer"`; else
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
  class ([known-debt.md](../known-debt.md) D1); `Organization`/`LeaveManagement`/`Location`/`Catalog`/`Currency`/`Orders`/`Inventory`/`Transfers`/`Purchasing`
  hold the DbContext logic directly; `Approval` splits by audience, with the write-path workflow rules on the
  `ApprovalRequest` aggregate behind a thin `IApprovalService`. See each module doc.
- **Audience-split controllers + real-time push** in `Notifications` — an admin controller (explicit
  permissions) and a self-service controller (permission-less, hard-scoped via `ICurrentUser`) over
  one table, plus a push-only SignalR hub at `/signalr-hub` (path configurable via `Notifications:Hub:Path`).
- **Cost visibility is one Inventory permission reused across modules.** Unit costs and stock values are
  Inventory's moving-average costs; every module that surfaces them (`Inventory`, `Transfers`,
  `Purchasing`'s return cost) masks the fields to null unless the caller has
  `inventory.stock.view_cost` (or full control), decided by a small per-module access helper at the
  controller.
- **Validation is two-layered FluentValidation, established by `Location` (first real per-module
  usage — the pipeline behavior and assembly-scan wiring already existed, but no module had a
  registered validator before it) and adopted identically by every later module.** Each `Contracts` request DTO carries its own
  `AbstractValidator<TRequest>` in the same file (field-shape rules only); each mediator command has a
  thin `AbstractValidator<TCommand>` (same file as the command+handler) validating route-level
  primitives directly and delegating to the Contracts validator via
  `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. Paired with the convention that
  domain aggregates hold only real invariants, not input-shape checks — see
  [../conventions/coding-conventions.md](../conventions/coding-conventions.md).

## Shared Kernel / Common Building Blocks

- **`src/Shared`** (leaf) — `ICurrentUser`/`IDateTime`, `CurrentUserBase` (claims-driven, incl. an
  `EmployeeId` accessor), `Status` value object, `PageQuery`/`IPage`/`SearchQuery`,
  `BaseDto`/`BaseDto<TId>`, `AffectedRowsResult`, entity wrappers, the mediator pipeline behaviors,
  permission authorization (`SuperUserPolicy`, `AccessControl`, internal
  `PolicyProvider`/`AuthorizationHandler`), constants (`ClaimTypeConstants` incl.
  `EmployeeId = "employee_id"`, `CronTimeConstants`, `CurrencyConstants.Default = "VND"` — a bootstrap/seed
  default only, the code the `Currency` module seeds as the base currency, not an enforced rule),
  `ReflectionHelper`, and `ValueObjects/Money`/`ValueObjects/VatPercentage` — guarded amount/currency
  and percentage value objects. `Money` is non-negative and accepts any ISO-4217-shaped currency code
  (trimmed, upper-cased); it has **no cross-currency guard or arithmetic**, so a caller combining amounts
  must check currency equality itself. Each has an `internal Update` that mutates the tracked instance in
  place (same pattern as `LeaveManagement`'s `DateRange.Update`); `Catalog.Api`'s `Product` aggregate
  and `Orders.Api`'s `Order`/`OrderLine`/`OrderFee`/`Payment` aggregates are their consumers of `Update`,
  each reached via its own scoped `InternalsVisibleTo` grant (`"StarterKit.Catalog.Api"`,
  `"StarterKit.Orders.Api"`) alongside the existing `InternalsVisibleTo("Framework.Tests")`. Covered by
  `tests/Framework.Tests/Shared/{Money,VatPercentage}Tests.cs`.
- **`src/Infrastructure`** (→ `Shared`) — CORS, health checks, Mapster config, module/endpoint base
  classes (`AppModule`, `AppModuleEndpoint`), API controller base classes + `BasicAuthAttribute`,
  static Serilog bootstrap (`AppLogging`).
- **`src/Persistence`** (→ `Shared`) — EF Core provider wiring (`DbContextExtensions`, `DbProvider`,
  `DbConnectionNames` — all aliasing `Default`), `BaseDbContext`,
  `TrackingExtensions`/`DispatchDomainEventsExtensions`, `IndexBuilderExtensions.HasProviderFilter`,
  paging/result helpers, migration-time runtime
  support, and an opt-in `Repositories/ICacheRepository<T>` — plus
  `IDynamicsDbCache<T>`/`DynamicTableRepository<T,TEntity,TContext>` for EAV/dynamic-column tables —
  whole-table cache-repository wrapper for small, infrequently-written reference/lookup tables.
  Explicitly **not** a replacement for the default direct-DbContext-plus-Specification access pattern
  every module uses; it has **zero adopters today** (`Location`'s `LocationTypeCache` was deliberately
  kept as a hand-written module-local cache instead of adopting it — see
  [modules/Location.md § Notable Conventions](modules/Location.md#notable-conventions)). See
  [known-debt.md](../known-debt.md) for the unenforced write-path-exclusivity contract this repository
  documents but does not enforce.
- **`<Module>.Contracts`** — the per-module seam. None is a true leaf (every one references `Shared`);
  `Shared` is the only true leaf.

## Module/Route Boundaries

`StarterKit.WebApi` wires all twelve modules via `app.MapEndpoints(...)`, maps the `Notifications`
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
| Inventory postings and their calling module's commit are two commits, not one transaction (`Orders`, `Transfers`, `Purchasing`) | Low–Medium | Recovered by idempotent retries plus each module's reconciliation sweep; residual risks are listed in [known-debt.md](../known-debt.md). |

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
