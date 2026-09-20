# Module Overview: Inventory

## Purpose

Owns physical stock quantities per product per location, via two aggregates sharing the module's one
`InventoryDbContext`. `StockLevel` is the current on-hand quantity for one `(ProductId, LocationId)`
pair — created lazily by the first positive movement, and never allowed to go negative: `Apply` is its
only mutation and throws a `ConflictException` if the resulting quantity would drop below zero (strict
no-oversell). `StockAdjustment` is a separate, immutable, append-only ledger entry recording every
signed stock movement (`ManualAdjustment`, `OrderPlacement`, `OrderCancellationRestore`, plus three
reserved-for-future reasons — `PurchaseReceipt`, `TransferIn`, `TransferOut`) — there is no update or
delete path; a correction is always a new entry, and a reversal references the original it undoes via
`ReversesAdjustmentId`. `StockLedger` (an internal domain service, not itself an aggregate) coordinates
both aggregates so a batch of movements commits in one `SaveChangesAsync`, keeping
`StockLevel.QuantityOnHand == sum(deltas)`; it aggregates every product/location shortfall in a batch
into a single `ConflictException` rather than failing on only the first insufficient line, and retries
the whole operation once from a clean change tracker on a concurrency conflict (a stale
`StockLevel.ConcurrencyToken` or a racing insert hitting the unique index).

Every location referenced by a movement is validated via `Location.Contracts.ILocationDirectoryService
.ExistsAsync` before any adjustment is written — Inventory is this seam's **second** consumer, after
`Orders`. `IInventoryService` (`DecrementForOrderAsync`/`RestoreForOrderAsync`) is Inventory's own
cross-module seam; it is consumed synchronously, in-process, by `Orders.Api`'s `PlaceOrderCommandHandler`/
`CancelOrderCommandHandler` (see Notable Conventions and [Orders.md](Orders.md)) — Inventory deliberately
does **not** subscribe to `Orders.Contracts.OrderPlacedIntegrationEvent`, because the strict no-oversell
requirement needs the decrement to reject a placement synchronously, which a best-effort async event
cannot guarantee.

## Internal Layering

Inventory is a **single-project module** (not split Domain/Application/Infrastructure/Api), following
the same structural convention as `Location`/`Catalog`/`Orders`:

| Project | Responsibility | Notes |
|---|---|---|
| `Inventory.Contracts` | DTOs, requests, enums, and the permission catalog, organized into per-feature subfolders — `Common/` (`StockMovementReason`: `ManualAdjustment`/`OrderPlacement`/`OrderCancellationRestore`/`PurchaseReceipt` (reserved)/`TransferIn` (reserved)/`TransferOut` (reserved)), `Stock/` (`StockLevelDto`, `StockAdjustmentDto`, `StockLine`, `ProductStockTotalDto`, `RecordStockMovementRequest` (carries its own `AbstractValidator` in the same file — the same two-layer FluentValidation convention `Location` established), `SearchStockLevelRequest : SearchQuery`, `SearchStockAdjustmentRequest : SearchQuery`), `Services/` (`IInventoryService` — the module's cross-module seam, see Notable Conventions), `Authorization/` (`InventoryPermissions`, `InventoryPermissionProvider`). Declares `Lightsoft.AspNetCore.Authorization` directly. |
| `Inventory.Api` | Single project organized by folder: `Domain/StockLevels/` — the `StockLevel` aggregate (private ctor; `Create` factory; `CanApply`/`Apply`; internal `RotateConcurrencyToken`). `Domain/StockAdjustments/` — the `StockAdjustment` aggregate (private ctor; `Create` factory; internal `ReleaseIdempotencyKey`) and `StockAdjustmentReason`, a domain-side enum mirroring `Contracts`' `StockMovementReason` (same names/values, kept separate so the domain layer doesn't reference a `Contracts` type directly; the query handlers cast between the two). `Data/` (`InventoryDbContext`, `InventoryContextInitialiser` — migrates only, no seed data, see Data Access). `Services/` (`StockLedger` — internal domain coordinator over both aggregates; `StockMovement` — internal record describing a requested signed change before it becomes a ledger entry; `InventoryService` — internal `IInventoryService` implementation). `Application/StockLevels/Queries` (`SearchStockLevels`, `GetProductStockTotal`), `Application/StockAdjustments/{Commands,Queries}` (`RecordStockMovement`; `SearchStockAdjustments`) — every handler owns its `InventoryDbContext`/`StockLedger` logic directly, plus a thin per-command `AbstractValidator`, same shape as `Orders`/`Location`/`Catalog`. `Controllers/` (`StockLevelController`, `StockAdjustmentController`). `InventoryModule.cs` (DI: DbContext, `StockLedger`, `IInventoryService`, permission provider). |

## Public Contract

`StockLevelController` (route `stock_level`, `[MustHavePermission(InventoryPermissions.Stock.View)]` at
class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/stock_level` | GET | `inventory.stock.view` | `SearchStockLevelRequest` (`ProductId?`, `LocationId?`, `SearchQuery` paging) | `PagedResult<StockLevelDto>`, ordered by `ProductId` then `LocationId` |
| `api/v{version}/stock_level/total/{productId}` | GET | `inventory.stock.view` | Route `productId` | `Result<ProductStockTotalDto>` — `TotalQuantityOnHand` (summed across all locations) and `LocationCount` (locations currently holding a positive quantity); returns a zeroed DTO rather than a 404 when no `StockLevel` row exists yet for the product |

`StockAdjustmentController` (route `stock_adjustment`, `[MustHavePermission(InventoryPermissions.Stock.View)]`
at class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/stock_adjustment` | GET | `inventory.stock.view` | `SearchStockAdjustmentRequest` (`ProductId?`, `LocationId?`, `SourceOrderId?`, `SearchQuery` paging) | `PagedResult<StockAdjustmentDto>`, ordered by `OccurredAt` desc then `Id` desc |
| `api/v{version}/stock_adjustment` | POST | `inventory.stock.manage` | `RecordStockMovementRequest { ProductId, LocationId, QuantityDelta, Note? }` | `IResult`; validates `LocationId` via `ILocationDirectoryService.ExistsAsync` (404 if missing), then applies a single `StockMovement` with `Reason = ManualAdjustment` through `StockLedger.ApplyAsync` — a resulting negative `StockLevel` throws `ConflictException` |

Every action across both controllers dispatches a mediator command/query under
`Application/{StockLevels,StockAdjustments}/{Commands,Queries}` — handlers own their `InventoryDbContext`
logic directly, same shape as `Orders`/`Location`/`Catalog`. `IInventoryService` has no HTTP surface of
its own — it is a DI-only seam consumed by `Orders.Api` (see Dependencies / Depended On By).

`InventoryPermissions.Stock` exposes only `View`/`Manage` — not the four-way
`View`/`Create`/`Update`/`Delete` split most other modules use, the same per-module simplification
`Location`/`Catalog`/`Orders` already use.

## Data Access

`InventoryDbContext : BaseDbContext`, schema `"inventory"`, registered via
`Persistence.DbContextExtensions.AddConfiguredDbContext<InventoryDbContext>(configuration, DbConnectionNames.Inventory)`.
`DbConnectionNames.Inventory` aliases `DbConnectionNames.Default` ("DefaultConnection") — same physical
database/connection string as every other module, separated only by schema (`inventory`) + table name.

Two tables, both keyed by a database-generated `bigint IDENTITY(1,1)` (`AuditableEntity<long>`):

- **`StockLevels`** — unique index on `(ProductId, LocationId)`. `ConcurrencyToken` is a required
  `MaxLength(32)` `IsConcurrencyToken()` column, app-managed: `InventoryDbContext.RotateConcurrencyTokens()`
  rotates it (`Guid("N")`) on every modified `StockLevel` during `SaveChanges[Async]` — same mechanism as
  `Order.ConcurrencyToken`. `LocationId` max length 450.
- **`StockAdjustments`** — index on `SourceOrderId`; index on `(ProductId, LocationId)`; unique index on
  `IdempotencyKey` (nullable-unique — SQL Server's EF provider adds the `IS NOT NULL` filter itself, so
  any number of rows may carry no key, same treatment as `Employee.UserId`). `LocationId` max length 450,
  `Note` max length 500, `PerformedByUserId` max length 450, `IdempotencyKey` max length 200.

Both entities call `entity.ConfigureAuditableEntity<TEntity, long>()`; `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: false)` and
rotates `StockLevel` concurrency tokens — no entity implements `ISoftDelete`, so this module has no
soft-delete support at all. Neither override dispatches domain events — this module raises no
`BaseEntity` domain events.

Query handlers read `AsNoTracking` and hand-map to DTOs directly (`SearchStockLevelsQueryHandler`,
`SearchStockAdjustmentsQueryHandler`, and `GetProductStockTotalQueryHandler`'s `GroupBy`/`Sum`
projection) — no Mapster.

`InventoryContextInitialiser.InitialiseAsync()` applies migrations only. Unlike `Orders`/`Location`/
`Catalog`, there is **no `TrySeedAsync`** — the module has no catalog-style reference data to seed.
`src/Migrations/MSSQL/Program.cs` calls it.

Migrations exist for **MSSQL only so far**: `src/Migrations/MSSQL/Inventory/` holds a single incremental
migration (`CreateInventorySchema`) — not yet a squashed baseline (per the dev-migration-squash
convention, squashing happens once a module is judged complete). The `PostgreSQL`/`Sqlite` migration
projects do not yet reference `Inventory.Api` at all.

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Inventory.Contracts → Shared`) | `BaseDto<long>` for `StockLevelDto`/`StockAdjustmentDto`; `AuditableEntity<long>` base for both aggregates. |
| `Infrastructure` | project (`Inventory.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Inventory.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity<TEntity, TId>`, `DbUpdateExceptionExtensions.IsUniqueConstraintViolation` (the retryable-conflict check in `StockLedger`). |
| `Location.Contracts` | project (`Inventory.Api → Location.Contracts`) | `ILocationDirectoryService.ExistsAsync`, consumed by `InventoryService.DecrementForOrderAsync` and `RecordStockMovementCommandHandler` to validate `LocationId` before any stock movement. Inventory is this seam's **second** consumer, after Orders (see [Location.md](Location.md)). |
| `Inventory.Contracts` | project (`Inventory.Api → Inventory.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result` | package, **all declared directly** | Same positive contrast as `Location`/`Catalog`/`Orders` — no undeclared-transitive-dependency instance. |

Inventory has **no outgoing dependency on `Orders`** — the relationship is one-way,
`Orders.Api → Inventory.Contracts` (see Depended On By).

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/MSSQL` — references `Inventory.Api` directly for `InventoryDbContext`/
  `InventoryContextInitialiser`. `PostgreSQL`/`Sqlite` do not (see Data Access).
- `Inventory.Tests` — `Inventory.Api.csproj` grants `InternalsVisibleTo` to reach the `internal`
  command/query records and handlers, plus a second grant to `DynamicProxyGenAssembly2` for mocking
  `ILocationDirectoryService` with Moq.
- **`Orders.Api`** — references `Inventory.Contracts` and calls `IInventoryService.DecrementForOrderAsync`/
  `RestoreForOrderAsync` synchronously from `PlaceOrderCommandHandler`/`CancelOrderCommandHandler` (see
  [Orders.md](Orders.md) and Notable Conventions below). Unlike `Location`/`Catalog`, Inventory is
  depended on by another **business module**, not just consumed passively by the host/migrations — but
  the dependency direction stays strictly one-way: Inventory has no reference back to `Orders`.

## Notable Conventions

- **Ledger-plus-level split: `StockAdjustment` is the immutable history, `StockLevel` is the running
  total.** `StockLedger` is the only place either is written, and always writes both together in one
  `SaveChangesAsync` so `StockLevel.QuantityOnHand` never drifts from `sum(StockAdjustment.QuantityDelta)`
  for that `(ProductId, LocationId)`. A missing `StockLevel` row counts as zero on hand — only a net
  positive movement is allowed to create one; a net negative movement against a non-existent level is
  treated as a shortfall like any other.
- **Shortfalls are aggregated across the whole batch, not reported on the first one found.**
  `StockLedger.ApplyPendingAsync` groups pending movements by `(ProductId, LocationId)`, evaluates every
  group, and collects every insufficient one into a single `ConflictException` message — so a multi-line
  order placement that would oversell two different products reports both at once, not just the first.
- **Idempotency is a nullable-unique `StockAdjustment.IdempotencyKey`.** An order-placement decrement key
  is `order-place:{orderId}:{lineId}`; a cancellation-restore key is
  `order-restore:{orderId}:{lineId}:{originalAdjustmentId}` — the restore key deliberately also encodes
  the *original* adjustment's own id, not just the order/line, so that a later re-place followed by
  another restore of the same order/line produces a distinct key each time (the original's `Id` differs)
  rather than colliding with an already-used restore key. `StockLedger.ApplyAsync` pre-filters out any
  movement whose key already exists before writing, so a retried or compensating call is a safe no-op.
  Reversing an adjustment also calls `StockAdjustment.ReleaseIdempotencyKey()` on the original, freeing
  its key so the same order line can be decremented again by a future re-placement.
- **A concurrency conflict retries once, from a clean change tracker.** `StockLedger.ExecuteWithRetryAsync`
  catches `DbUpdateConcurrencyException` or a unique-constraint-violating `DbUpdateException`
  (`IsUniqueConstraintViolation()`), clears the change tracker, and retries the whole operation exactly
  once before surfacing a `ConflictException("Stock was modified concurrently. Please retry.")`.
- **Every `LocationId` is validated cross-module before it is trusted**, both on the manual-adjustment
  path (`RecordStockMovementCommandHandler`) and the order-driven path (`InventoryService
  .DecrementForOrderAsync`) — via `Location.Contracts.ILocationDirectoryService.ExistsAsync`. Inventory
  is this seam's second consumer after Orders (see [Location.md](Location.md)).
- **Cross-module stock movement is a synchronous, DI-only seam (`IInventoryService`), not an
  integration-event subscription — a deliberate departure from the event-based pattern `Approval`/
  `LeaveManagement` established.** `Orders.Api`'s `PlaceOrderCommandHandler` calls
  `IInventoryService.DecrementForOrderAsync` in-process, inside the same request, *before* its own
  `SaveChangesAsync` — an oversell throws a `ConflictException` synchronously, so nothing is persisted in
  `Orders` and the caller gets an immediate, accurate rejection. A best-effort, post-commit
  `OrderPlacedIntegrationEvent` (as `Approval`'s `ApprovalFinalizedIntegrationEvent` or `Identity`'s
  integration events are consumed) cannot provide that guarantee — by the time such a handler ran, the
  order would already be committed as `Placed`. Inventory therefore has no handler for
  `OrderPlacedIntegrationEvent`, and none is planned; the event is still published for any other future
  consumer, but Inventory is not one. See [Orders.md](Orders.md) for the full call sequence, including
  the compensating `RestoreForOrderAsync` call `PlaceOrderCommandHandler` makes if its own
  `SaveChangesAsync` fails after the decrement already committed.
- **Accepted crash-window risk between Inventory's commit and Orders' commit.** `IInventoryService
  .DecrementForOrderAsync` commits its own `SaveChangesAsync` inside `StockLedger` before
  `PlaceOrderCommandHandler` calls its own — a process crash between the two leaves stock reserved
  against an order still sitting in `Draft`. A retry of the same `PlaceOrder` call is safe (the decrement
  is idempotent via `IdempotencyKey`) and self-heals; no reconciliation `BackgroundService` exists for
  this window. See [../../known-debt.md](../../known-debt.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-20_
