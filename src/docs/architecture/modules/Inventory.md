# Module Overview: Inventory

## Purpose

Owns physical stock quantities and their value per product per location, via two aggregates sharing the
module's one `InventoryDbContext`. `StockLevel` is the current on-hand quantity and total value for one
`(ProductId, LocationId)` pair — created lazily by the first positive movement, and never allowed to go
negative: `Apply` is its only mutation and throws an `InsufficientStockException` (a `ConflictException`)
if the resulting quantity would drop below zero (strict no-oversell). `StockAdjustment` is a separate,
immutable, append-only ledger entry recording every signed stock movement — there is no update or delete
path; a correction is always a new entry, and a reversal references the original it undoes via
`ReversesAdjustmentId`. `StockLedger` (an internal domain service, not itself an aggregate) coordinates
both aggregates so a batch of movements commits in one `SaveChangesAsync`, keeping
`StockLevel.QuantityOnHand == sum(deltas)` and `StockLevel.TotalValueBase == sum(ValueDeltaBase)`; it
aggregates every product/location shortfall in a batch into a single `InsufficientStockException`
rather than failing on only the first insufficient line, and retries the whole operation once from a
clean change tracker on a concurrency conflict (a stale `StockLevel.ConcurrencyToken` or a racing insert
hitting the unique index).

Stock is valued at a **moving weighted average in base currency** (see Notable Conventions). Every
ledger entry carries its unit cost and signed value change; the level carries the running total value;
the average unit cost is derived, never stored. Costs are plain base-currency decimals; the module has no
dependency on `Currency`.

Every location referenced by a movement is validated via `Location.Contracts.ILocationDirectoryService
.ExistsAsync` before any adjustment is written. `IInventoryService` is Inventory's own cross-module seam,
consumed synchronously and in-process by `Orders`, `Transfers`, and `Purchasing` (see Notable
Conventions, [Orders.md](Orders.md), [Transfers.md](Transfers.md), [Purchasing.md](Purchasing.md)) —
Inventory deliberately does **not** subscribe to `Orders.Contracts.OrderPlacedIntegrationEvent`, because
the strict no-oversell requirement needs the decrement to reject a placement synchronously, which a
best-effort async event cannot guarantee.

## Internal Layering

Inventory is a **single-project module** (not split Domain/Application/Infrastructure/Api), following
the same structural convention as `Location`/`Catalog`/`Orders`:

| Project | Responsibility | Notes |
|---|---|---|
| `Inventory.Contracts` | DTOs, requests, enums, the cross-module seam, a typed exception, and the permission catalog, organized into per-feature subfolders — `Common/` (`StockMovementReason`, `StockSourceType`), `Stock/` (level/adjustment/valuation DTOs, the seam's line and result records, and the manual-movement/revaluation/search requests — each request carries its own `AbstractValidator` in the same file, the two-layer FluentValidation convention `Location` established), `Services/` (`IInventoryService`), `Exceptions/` (`InsufficientStockException`), `Authorization/` (`InventoryPermissions`, `InventoryPermissionProvider`). Declares `Lightsoft.AspNetCore.Authorization` directly. |
| `Inventory.Api` | Single project organized by folder: `Domain/StockLevels/` — the `StockLevel` aggregate (private ctor; `Create` factory; the valuation arithmetic and `CanApply`/`Apply`; internal `RotateConcurrencyToken`). `Domain/StockAdjustments/` — the `StockAdjustment` aggregate (private ctor; `Create` factory enforcing per-reason source/quantity rules; internal `ReleaseIdempotencyKey`) and `StockAdjustmentReason`, a domain-side enum mirroring `Contracts`' `StockMovementReason` (kept separate so the domain layer doesn't reference a `Contracts` type directly; the query handlers cast between the two). `Data/` (`InventoryDbContext`, `InventoryContextInitialiser` — migrates only, no seed data). `Services/` (`StockLedger` — internal coordinator over both aggregates, including pricing; `StockMovement` — internal record describing a requested signed change before it becomes a ledger entry; `InventoryService` — internal `IInventoryService` implementation). `Application/StockLevels/Queries` and `Application/StockAdjustments/{Commands,Queries}` — every handler owns its `InventoryDbContext`/`StockLedger` logic directly, plus a thin per-command `AbstractValidator`, same shape as `Orders`/`Location`/`Catalog`. `Controllers/` (`StockLevelController`, `StockAdjustmentController`, and `InventoryCostAccess` — the single rule deciding whether a caller may see costs). `InventoryModule.cs` (DI: DbContext, `StockLedger`, `IInventoryService`, permission provider). |

## Public Contract

`StockLevelController` (route `stock_level`, `[MustHavePermission(InventoryPermissions.Stock.View)]` at
class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/stock_level` | GET | `inventory.stock.view` | `SearchStockLevelRequest` (`ProductId?`, `LocationId?`, `SearchQuery` paging) | `PagedResult<StockLevelDto>`, ordered by `ProductId` then `LocationId`; `AverageCostBase`/`TotalValueBase` are null unless the caller has `view_cost` |
| `api/v{version}/stock_level/total/{productId}` | GET | `inventory.stock.view` | Route `productId` | `Result<ProductStockTotalDto>` — `TotalQuantityOnHand` (summed across all locations) and `LocationCount` (locations currently holding a positive quantity); returns a zeroed DTO rather than a 404 when no `StockLevel` row exists yet for the product |
| `api/v{version}/stock_level/valuation` | GET | `inventory.stock.view_cost` (on top of the class-level view gate) | `SearchStockValuationRequest` (`ProductId?`, `LocationId?`, paging) | `Result<StockValuationDto>` — one page of per product/location lines (only levels holding stock) plus grand totals (quantity and base-currency value) over every matching line, not just the page |

`StockAdjustmentController` (route `stock_adjustment`, `[MustHavePermission(InventoryPermissions.Stock.View)]`
at class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/stock_adjustment` | GET | `inventory.stock.view` | `SearchStockAdjustmentRequest` (`ProductId?`, `LocationId?`, `SourceOrderId?`, `SearchQuery` paging) | `PagedResult<StockAdjustmentDto>`, ordered by `OccurredAt` desc then `Id` desc; `UnitCostBase`/`ValueDeltaBase` are null unless the caller has `view_cost`. The DTO exposes `SourceOrderId`/`SourceOrderLineId` only for `Order`-sourced entries (the generic source columns are not surfaced) |
| `api/v{version}/stock_adjustment` | POST | `inventory.stock.manage` | `RecordStockMovementRequest { ProductId, LocationId, QuantityDelta, UnitCost?, Note? }` | `IResult`; validates `LocationId` via `ILocationDirectoryService.ExistsAsync` (404 if missing), then applies one `ManualAdjustment` movement through `StockLedger.ApplyAsync`. `UnitCost` applies to inbound movements only and needs no permission beyond `manage` — an outbound movement is always at the current average, and an inbound one with no `UnitCost` is valued at the current average (rejected when nothing is on hand) |
| `api/v{version}/stock_adjustment/revaluation` | POST | `inventory.stock.revalue` | `RevalueStockRequest` (product, location, new unit cost, note) | `IResult`; writes a quantity-neutral `CostRevaluation` entry that re-prices the on-hand stock at the new unit cost; rejected when nothing is on hand |

Every action across both controllers dispatches a mediator command/query under
`Application/{StockLevels,StockAdjustments}/{Commands,Queries}`. `IInventoryService` has no HTTP surface
of its own — it is a DI-only seam (see Notable Conventions).

`InventoryPermissions.Stock` exposes `View`/`Manage` plus two cost-specific permissions, `ViewCost` and
`Revalue` — not the four-way `View`/`Create`/`Update`/`Delete` split most other modules use, the same
per-module simplification `Location`/`Catalog`/`Orders` use. `InventoryCostAccess` treats full control
like `[MustHavePermission]` does; Transfers and Purchasing reuse the `view_cost` permission for their own
cost masking (see [Transfers.md](Transfers.md), [Purchasing.md](Purchasing.md)).

## Data Access

`InventoryDbContext : BaseDbContext`, schema `"inventory"`, registered via
`Persistence.DbContextExtensions.AddConfiguredDbContext<InventoryDbContext>(configuration, DbConnectionNames.Inventory)`.
`DbConnectionNames.Inventory` aliases `DbConnectionNames.Default` ("DefaultConnection") — same physical
database/connection string as every other module, separated only by schema (`inventory`) + table name.

Two tables, both keyed by a database-generated `bigint IDENTITY(1,1)` (`AuditableEntity<long>`):

- **`StockLevels`** — unique index on `(ProductId, LocationId)`. `ConcurrencyToken` is a required
  `MaxLength(32)` `IsConcurrencyToken()` column, app-managed: `InventoryDbContext.RotateConcurrencyTokens()`
  rotates it (`Guid("N")`) on every modified `StockLevel` during `SaveChanges[Async]` — same mechanism as
  `Order.ConcurrencyToken`. `LocationId` max length 450. `TotalValueBase` is `decimal(19,4)`.
- **`StockAdjustments`** — index on `(SourceType, SourceId)`; index on `ReversesAdjustmentId` (serves the
  "not yet reversed" anti-join); index on `(ProductId, LocationId)`; unique index on `IdempotencyKey`
  (nullable-unique — any number of rows may carry no key, same treatment as `Employee.UserId`).
  `SourceType`/`SourceId`/`SourceLineId` are
  nullable generic source columns (a manual adjustment has none). `UnitCostBase`/`ValueDeltaBase` are
  `decimal(19,4)`. `LocationId` max length 450, `Note` max length 500, `PerformedByUserId` max length 450,
  `IdempotencyKey` max length 200.

Both entities call `entity.ConfigureAuditableEntity<TEntity, long>()`; `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: false)` and
rotates `StockLevel` concurrency tokens — no entity implements `ISoftDelete`, so this module has no
soft-delete support at all. Neither override dispatches domain events — this module raises no
`BaseEntity` domain events.

Query handlers read `AsNoTracking` and hand-map to DTOs directly — no Mapster. The valuation query sums
the two needed columns in memory (EF Core's Sqlite provider cannot translate `SUM` over a decimal) and
pages the line list in SQL.

`InventoryContextInitialiser.InitialiseAsync()` applies migrations only. Unlike `Orders`/`Location`/
`Catalog`, there is **no `TrySeedAsync`** — the module has no catalog-style reference data to seed. Each
provider's migrator `Program.cs` calls it. Migrations: see
[../../conventions/migrations.md](../../conventions/migrations.md).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Inventory.Contracts → Shared`) | `BaseDto<long>` for the DTOs; `AuditableEntity<long>` base for both aggregates. |
| `Infrastructure` | project (`Inventory.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Inventory.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity<TEntity, TId>`, paging extensions, `DbUpdateExceptionExtensions.IsUniqueConstraintViolation` (the retryable-conflict check in `StockLedger`). |
| `Location.Contracts` | project (`Inventory.Api → Location.Contracts`) | `ILocationDirectoryService.ExistsAsync`, consumed by `InventoryService` and the manual-movement/revaluation handlers to validate `LocationId` before any stock movement (see [Location.md](Location.md)). |
| `Inventory.Contracts` | project (`Inventory.Api → Inventory.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result` | package, **all declared directly** | Same positive contrast as `Location`/`Catalog`/`Orders` — no undeclared-transitive-dependency instance. |

Inventory has **no outgoing dependency on** `Orders`, `Transfers`, `Purchasing`, or `Currency` — every
relationship is one-way, toward `Inventory.Contracts` (see Depended On By).

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` — each references `Inventory.Api` directly for
  `InventoryDbContext`/`InventoryContextInitialiser`.
- `Inventory.Tests` — `Inventory.Api.csproj` grants `InternalsVisibleTo` to reach the `internal`
  command/query records and handlers, plus a second grant to `DynamicProxyGenAssembly2` for mocking
  `ILocationDirectoryService` with Moq.
- **`Orders.Api`** — `IInventoryService.DecrementForOrderAsync`/`RestoreForOrderAsync`/
  `FilterSourceIdsWithUnreversedPostingsAsync`, from `PlaceOrderCommandHandler`/`CancelOrderCommandHandler`
  and the orphaned-stock reconciliation sweep (see [Orders.md](Orders.md)).
- **`Transfers.Api`** — `IssueStockAsync` (dispatch), `ReceiveStockAsync` (receipt), and
  `FilterSourceIdsWithUnreversedPostingsAsync` (see [Transfers.md](Transfers.md)).
- **`Purchasing.Api`** — `ReceiveStockAsync` (goods receipt), `IssueStockAsync` (purchase return), and
  `FilterSourceIdsWithUnreversedPostingsAsync` (see [Purchasing.md](Purchasing.md)).

The dependency direction stays strictly one-way: Inventory has no reference back to any of them.

## Notable Conventions

- **Ledger-plus-level split: `StockAdjustment` is the immutable history, `StockLevel` is the running
  total.** `StockLedger` is the only place either is written, and always writes both together in one
  `SaveChangesAsync` so `StockLevel.QuantityOnHand` never drifts from `sum(StockAdjustment.QuantityDelta)`
  (nor `TotalValueBase` from `sum(ValueDeltaBase)`) for that `(ProductId, LocationId)`. A missing
  `StockLevel` row counts as zero on hand — only a net positive movement is allowed to create one; a net
  negative movement against a non-existent level is treated as a shortfall like any other.
- **Moving weighted-average valuation in base currency.** `StockLevel.TotalValueBase` is the stored value;
  `AverageCostBase` is derived (`TotalValueBase / QuantityOnHand`, rounded to 4 decimals, zero when empty).
  An inbound movement is valued at its supplied unit cost (or, when none is supplied and stock is on hand,
  the current average); an outbound movement at the current average — issuing the entire on-hand quantity
  removes the entire remaining value so rounding residue never lingers on an empty level; a reversal is
  valued at exactly its original's value; a `CostRevaluation` is quantity-neutral and re-prices the on-hand
  stock to `on-hand × new unit cost`. `StockLevel.Apply` refuses a negative value and an empty level that
  still holds value. `StockAdjustment.UnitCostBase`/`ValueDeltaBase` are the authoritative per-entry record.
- **Shortfalls are aggregated across the whole batch, not reported on the first one found.**
  `StockLedger` groups pending movements by `(ProductId, LocationId)`, evaluates every group, and
  collects every insufficient one into a single `InsufficientStockException` message — so a multi-line
  order placement that would oversell two different products reports both at once.
  `InsufficientStockException : ConflictException` lives in `Inventory.Contracts` so it still maps to
  409 and existing `catch (ConflictException)` blocks keep working, while a caller (Transfers, Purchasing)
  can tell a deterministic shortage from the transient "Stock was modified concurrently" conflict without
  inspecting the message.
- **Generic source columns.** A `StockAdjustment` may carry a nullable `(SourceType, SourceId,
  SourceLineId)` pointing at the originating document (`StockSourceType`: `Order`, `GoodsReceipt`,
  `Transfer`, `PurchaseReturn`, `TransferReceipt`). `StockAdjustment.Create` requires the source type
  matching each source-driven reason (`OrderPlacement`/`OrderCancellationRestore` → `Order`,
  `PurchaseReceipt` → `GoodsReceipt`, `TransferIn` → `TransferReceipt`, `TransferOut` → `Transfer`,
  `PurchaseReturnOut` → `PurchaseReturn`); `ManualAdjustment` and `CostRevaluation` have none.
- **Idempotency is a nullable-unique `StockAdjustment.IdempotencyKey`.** Keys: an order-placement
  decrement is `order-place:{orderId}:{lineId}`; a cancellation restore is
  `order-restore:{orderId}:{lineId}:{originalAdjustmentId}` — it deliberately also encodes the *original*
  adjustment's own id so a later re-place followed by another restore produces a distinct key each time;
  seam receives/issues use `goods-receipt:`, `transfer-receipt:`, `transfer-issue:`, and `purchase-return:`
  prefixes over `{sourceId}:{caller line reference}`. `StockLedger.ApplyAsync` pre-filters out any movement
  whose key already exists before writing, so a retried or compensating call is a safe no-op, and the
  seam's returned `StockPostingResult`s are recomputed from the stored adjustments so a replay returns the
  originally posted costs. Reversing an adjustment also calls `StockAdjustment.ReleaseIdempotencyKey()` on
  the original, freeing its key so the same order line can be decremented again by a future re-placement.
- **A concurrency conflict retries once, from a clean change tracker.** `StockLedger.ExecuteWithRetryAsync`
  catches `DbUpdateConcurrencyException` or a unique-constraint-violating `DbUpdateException`
  (`IsUniqueConstraintViolation()`), clears the change tracker, and retries the whole operation exactly
  once before surfacing a `ConflictException("Stock was modified concurrently. Please retry.")`.
- **Every `LocationId` is validated cross-module before it is trusted**, on the manual-adjustment and
  revaluation paths and on every `IInventoryService` posting — via
  `Location.Contracts.ILocationDirectoryService.ExistsAsync`.
- **Cross-module stock movement is a synchronous, DI-only seam (`IInventoryService`), not an
  integration-event subscription — a deliberate departure from the event-based pattern `Approval`/
  `LeaveManagement` established.** The seam is:
  - `DecrementForOrderAsync` — all-or-nothing decrement for a placed order at the current average;
  - `RestoreForOrderAsync(orderId, user, ct, postedBefore?)` — writes a reversal per not-yet-reversed
    order-placement adjustment; with `postedBefore`, only adjustments written at or before that instant
    are reversed, so a placement committed later by a concurrent `PlaceOrder` is never undone;
  - `FilterSourceIdsWithUnreversedPostingsAsync(sourceType, ids, postedBefore, ct)` — which of the given
    source documents still hold a posted, unreversed adjustment at or before the cutoff (the posting reason
    is derived from the source type);
  - `ReceiveStockAsync` — inbound at caller-supplied base-currency unit costs; accepts only
    `GoodsReceipt`/`TransferReceipt` sources;
  - `IssueStockAsync` — all-or-nothing outbound at the current average with strict no-oversell; accepts
    only `Transfer`/`PurchaseReturn` sources.
  Every posting call takes a per-line idempotency reference (validated non-blank, unique per document,
  short enough to fit the key column). Only `Order` placements are ever reversed; documents posted through
  `ReceiveStockAsync`/`IssueStockAsync` are never reversed. Because the caller decides what happens on a
  refusal, an oversell is surfaced synchronously before the caller commits — the guarantee a best-effort
  async event cannot provide. See [Orders.md](Orders.md) for the order call sequence and
  [Transfers.md](Transfers.md)/[Purchasing.md](Purchasing.md) for the two-commit posting shape those
  modules build on top of it.
- **Seam limitation: a single call posts either only inbound or only outbound lines.** Mixing inbound and
  outbound movements for the same product/location in one batch is not supported — only the net delta is
  availability-checked, and pricing order inside the group is an implementation detail. A cost revaluation
  against a level that does not exist yet is rejected even if an inbound in the same batch would create
  stock.
- **Cost visibility and cost-setting are permission-gated, and the visibility rule lives in one place.**
  `inventory.stock.view_cost` reveals unit costs, values, and the valuation endpoint (cost fields are null
  otherwise); `inventory.stock.revalue` gates only the revaluation endpoint. A manual inbound adjustment may
  carry an explicit `UnitCost` with `inventory.stock.manage` alone — that is how a product is first
  stocked, since there is no average to default to when nothing is on hand. Legacy stock that existed
  before valuation was introduced has value 0 until revalued.
- **Accepted crash-window risk between Inventory's commit and the caller's commit.** Each posting commits
  inside `StockLedger` before the calling module commits its own document — a process crash between the two
  leaves stock posted against a document still in its pre-commit state. Every call is idempotent, so a
  retry self-heals; each consuming module runs a periodic reconciliation sweep for the case where no retry
  arrives (see [Orders.md](Orders.md), [Transfers.md](Transfers.md), [Purchasing.md](Purchasing.md)).
  Residual risks are in [../../known-debt.md](../../known-debt.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
