# Module Overview: Transfers

## Purpose

Owns moving stock between two locations with an explicit **in-transit** phase, via one aggregate
(`StockTransfer`, with `TransferLine` and `TransferReceipt`/`TransferReceiptLine` children) in the
module's one `TransfersDbContext`. The transfer's source and destination are opaque `Location` ids
(resolved through `Location.Contracts.ILocationDirectoryService`, snapshotted with their names, not
FK-constrained), it has a unique system-generated `TransferCode`, and its `Status` is a state machine:

`Draft` → `Posting` → `Dispatched` → `PartiallyReceived` → `Received`; `Dispatched`/`PartiallyReceived` →
`Closed`; `Draft` → `Cancelled`.

- A draft is edited (header and lines), then **dispatched**: stock leaves the source location through
  `IInventoryService.IssueStockAsync` (all-or-nothing, strict no-oversell), and each line freezes the
  moving-average unit cost Inventory issued it at. `Posting` is the short-lived internal state around that
  call; an Inventory refusal returns the transfer to `Draft`.
- A dispatched transfer is **received** in one or more deliveries (`TransferReceipt`, identified by a
  caller-supplied `ClientRequestId` so a double-submit never receives twice). Each receipt lands at the
  destination through `IInventoryService.ReceiveStockAsync` at the line's **frozen** dispatch cost, so the
  inbound leg enters at exactly the cost that left the source. Over-receipt is refused per line against
  what is still in transit, counting receipts that are recorded but not yet applied.
- **Close** gives up the undelivered remainder: it is written off as a local variance
  (`QtyClosedShort` and its value) — no stock is posted to Inventory. **A dispatched transfer can never be
  cancelled**, only closed; only a `Draft` can be cancelled.

Stock movements are posted by the command handlers around the aggregate's state changes; the aggregate
only tracks resulting quantities and costs. Cost data is Inventory's moving-average cost (base currency),
so it is masked behind Inventory's `view_cost` permission (see Notable Conventions). The module has no
dependency on `Currency`.

## Internal Layering

Transfers is a **single-project module** (not split Domain/Application/Infrastructure/Api), the same
structural convention as `Location`/`Catalog`/`Orders`/`Inventory`:

| Project | Responsibility | Notes |
|---|---|---|
| `Transfers.Contracts` | DTOs, requests, enums (`TransferStatus`, `TransferReceiptStatus`), and the permission catalog, in per-feature subfolders (`Common/`, `StockTransfers/`, `Authorization/`). Each request carries its own `AbstractValidator` in the same file. Declares `Lightsoft.AspNetCore.Authorization` directly; references `Shared`. Exposes no cross-module seam and no events. |
| `Transfers.Api` | Single project organized by folder: `Domain/StockTransfers/` — the `StockTransfer` aggregate (private ctor; `Create` factory; draft-only header/line editors; `BeginDispatch`/`CompleteDispatch`/`AbortDispatch`; `BeginReceive`/`CompleteReceive`/`VoidReceipt`; `Close`; `Cancel`), its `TransferLine`/`TransferReceipt` children (mutators `internal`, driven only by the root), the `TransferCode` value object, `StockTransferByIdSpec`, and domain events (dispatched, no handler yet). `Data/` (`TransfersDbContext`, `TransfersContextInitialiser` — migrates only, no seed data). `Services/` (`TransferLocationResolver`). `Application/StockTransfers/{Commands,Queries}` — handlers own their `TransfersDbContext` logic directly, plus a thin per-command `AbstractValidator`; alongside them the shared `TransferPosting` orchestration, `TransferSaving`, and the `TransfersPostingReconciliationService` background sweep with its options/state. `Controllers/` (`StockTransferController`, `TransferCostAccess`). `TransfersModule.cs` (DI: DbContext, `TransferLocationResolver`, permission provider, reconciliation options + hosted service). |

## Public Contract

`StockTransferController` (route `stock_transfer`, `[MustHavePermission(TransfersPermissions.Transfers.View)]`
at class level; every route id is `long`):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/stock_transfer` | GET | `transfers.transfers.view` | `SearchStockTransferRequest` (`Status?`, `SourceLocationId?`, `DestinationLocationId?`, paging) | `PagedResult<StockTransferDto>`; cost fields null unless the caller has Inventory's `view_cost` |
| `api/v{version}/stock_transfer/{id}` | GET | `transfers.transfers.view` | Route `id` | `Result<StockTransferDto>`, with lines and receipts; cost fields masked the same way |
| `api/v{version}/stock_transfer` | POST | `transfers.transfers.create` | `CreateStockTransferRequest { SourceLocationId, DestinationLocationId, Note? }` | `Result<long>`; both locations must exist and the destination must be active; source and destination must differ |
| `api/v{version}/stock_transfer/{id}` | PUT | `transfers.transfers.create` | `UpdateStockTransferRequest` | `Result`; edits the header (draft-only) |
| `api/v{version}/stock_transfer/{id}/line` | POST | `transfers.transfers.create` | `AddStockTransferLineRequest { ProductId, Quantity }` | `Result`; product existence checked through `ICatalogPricingService` (name/SKU snapshot; an inactive product can still be moved); one line per product (draft-only) |
| `api/v{version}/stock_transfer/{id}/line/{lineId}` | PUT / DELETE | `transfers.transfers.create` | `UpdateStockTransferLineRequest` / route ids | `Result`; draft-only |
| `api/v{version}/stock_transfer/{id}/dispatch` | PUT | `transfers.transfers.dispatch` | Route `id` | `Result`; two-commit dispatch (see Notable Conventions); an Inventory refusal is rethrown to the caller |
| `api/v{version}/stock_transfer/{id}/receipt` | POST | `transfers.transfers.receive` | `ReceiveStockTransferRequest { ClientRequestId, Lines[{ TransferLineId, Quantity }] }` | `Result<long>` (receipt id); idempotent per `ClientRequestId` |
| `api/v{version}/stock_transfer/{id}/close` | PUT | `transfers.transfers.close` | `CloseStockTransferRequest { Reason }` | `Result`; writes off the in-transit remainder locally |
| `api/v{version}/stock_transfer/{id}/cancel` | PUT | `transfers.transfers.create` | `CancelStockTransferRequest { Reason }` | `Result`; draft-only |

`TransfersPermissions.Transfers` exposes `View`/`Create`/`Dispatch`/`Receive`/`Close` — a per-action split
rather than the four-way CRUD split (`Create` also covers editing and cancelling a draft).

## Data Access

`TransfersDbContext : BaseDbContext`, schema `"transfers"`, registered via
`AddConfiguredDbContext<TransfersDbContext>(configuration, DbConnectionNames.Transfers)`;
`DbConnectionNames.Transfers` aliases `Default` — the same physical database as every other module,
separated by schema + table name.

Four tables, all `bigint IDENTITY(1,1)` keys (`AuditableEntity<long>`): `StockTransfers`, `TransferLines`,
`TransferReceipts`, `TransferReceiptLines`. Notable configuration:

- `StockTransfers` — unique `TransferCode` (a `HasConversion` scalar, not an owned type, same treatment as
  `Orders`' `OrderCode`); indexes on source and destination location; `(Status, Created)`; and a filtered
  index on `PostingStartedAt` (where not null, written with `HasProviderFilter`) that serves the
  reconciliation sweep. `ConcurrencyToken` is
  a required app-managed token. `Lines`/`Receipts` are cascade `HasMany`/`WithOne` with field-backed
  navigations. Location ids max length 450, names 200.
- `TransferLines` — `UnitCostBase` `decimal(19,4)`; `QtyInTransit` and `ClosedShortValueBase` are computed
  and ignored by EF.
- `TransferReceipts` — unique `(TransferId, ClientRequestId)`; `(Status, Created)` for the sweep.

`TransfersDbContext.RotateConcurrencyTokens()` rotates the **parent transfer's** token whenever the transfer
itself, or any of its lines, receipts, or receipt lines, is added/changed/removed — unlike `Orders`, which
rotates only on the root's own changes. A receipt is a pure child insert, and two concurrent receipts must
still collide on the parent's token or both could pass the over-receipt guard.

`SaveChangesAsync` audits, rotates tokens, and dispatches domain events post-commit (wrapped in
try/catch-log, same shape as `OrdersDbContext`); the synchronous `SaveChanges` stays audit + rotation only.
The aggregate raises dispatched/received/closed/cancelled events, but no handler in any module subscribes
to them. No entity implements `ISoftDelete`. Queries read `AsNoTracking` and hand-map to DTOs.

`TransfersContextInitialiser.InitialiseAsync()` applies migrations only; each provider's migrator
`Program.cs` calls it. Migrations: see [../../conventions/migrations.md](../../conventions/migrations.md).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Transfers.Contracts → Shared`) | `BaseDto<long>`, `AuditableEntity<long>`, `ICurrentUser`/`IDateTime`, permission helpers (`HasPermission`/`IsFullControl`). |
| `Infrastructure` | project (`Transfers.Api → Infrastructure`) | `VersionedApiController`, `AppModule`. |
| `Persistence` | project (`Transfers.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, audit tracking, domain-event dispatch, `HasProviderFilter`, paging, `IsUniqueConstraintViolation`. |
| `Inventory.Contracts` | project (`Transfers.Api → Inventory.Contracts`) | `IInventoryService` (`IssueStockAsync`, `ReceiveStockAsync`, `FilterSourceIdsWithUnreversedPostingsAsync`), `InsufficientStockException`, and `InventoryPermissions.Stock.ViewCost` for cost masking. |
| `Location.Contracts` | project (`Transfers.Api → Location.Contracts`) | `ILocationDirectoryService.GetAsync`, through `TransferLocationResolver`. |
| `Catalog.Contracts` | project (`Transfers.Api → Catalog.Contracts`) | `ICatalogPricingService.GetPriceInfoAsync`, for the product name/SKU snapshot on a new line. |
| `Transfers.Contracts` | project (`Transfers.Api → Transfers.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result` | package, all declared directly | No undeclared-transitive-dependency instance. |

## Depended On By

- `StarterKit.WebApi` — composition-root host (`ConfigureExtensions.cs`'s `assemblies` array).
- `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` — each references `Transfers.Api` for the `DbContext`/initialiser.
- `Transfers.Tests` — `Transfers.Api.csproj` grants `InternalsVisibleTo` for the test project plus
  `DynamicProxyGenAssembly2` for Moq.

No business module references `Transfers.Api`/`Transfers.Contracts`.

## Notable Conventions

- **Posting-first, two-commit sequences (no shared transaction with Inventory).** Both dispatch and receive
  need an id to post under, so the document is committed first in a `Posting` state, then Inventory is
  called, then a second commit records the outcome. Dispatch: commit the transfer as `Posting` →
  `IssueStockAsync` → `CompleteDispatch` (freezes each line's quantity and unit cost) → commit. Receive:
  commit the receipt as `Posting` → `ReceiveStockAsync` at the frozen cost → `CompleteReceive` (applies
  quantities, marks the receipt `Posted`) → commit. The shared `TransferPosting` class holds the second
  half so the command handlers and the sweep behave identically. Every Inventory call is idempotent per
  line, so repeating it returns the originally posted costs instead of posting twice.
- **Refusal versus transient failure.** After a failed post, the handler asks Inventory whether any postings
  landed anyway (`FilterSourceIdsWithUnreversedPostingsAsync` with "now" as the cutoff); if so it rolls
  forward. Only when nothing landed **and** the failure is a deterministic refusal (an
  `InsufficientStockException` or a validation failure — not the transient "Stock was modified
  concurrently" conflict) is a dispatch aborted back to `Draft` or a receipt voided (a receipt can only be
  refused by validation, since inbound cannot fail for shortage). A transient failure leaves the document in
  `Posting` for the sweep. **Nothing posted to Inventory is ever reversed.**
- **`TransfersPostingReconciliationService`** is the module-owned `BackgroundService` for a process that died
  between the Inventory call and the second commit. Options `Transfers:PostingReconciliation` (validated on
  start, every default in code): enabled, interval, a minimum time in `Posting` before pickup (so an
  in-flight request is never raced), batch size. Each tick, in a fresh DI scope, it pages transfers and
  receipts stuck in `Posting` by id with an in-memory watermark and finishes them through `TransferPosting`;
  a concurrency failure means the original request finished first. Inventory writes are attributed to
  `system:transfers-reconciliation`.
- **Receive idempotency is per `ClientRequestId`** (trimmed, case-insensitive, unique per transfer). A repeat
  returns the receipt already recorded — a concurrent duplicate that loses the insert race is resolved the
  same way — and only finishes it if still `Posting`; a voided receipt is a conflict that tells the caller
  to submit a new client request id.
- **Cost masking.** Unit costs and the closed-short value are Inventory's moving-average costs, so they are
  null in the DTOs unless the caller has `inventory.stock.view_cost` (or full control), decided by
  `TransferCostAccess` at the controller. Transfers defines no cost permission of its own.
- **Generated `TransferCode`** (`T` + `yyyyMMdd` + 9-char Crockford Base32) modeled on `Orders`'
  `OrderCode`; creation retries a unique-index collision a bounded number of times.
- **Optimistic concurrency covers the whole aggregate** through the parent token rotation described in Data
  Access; command handlers translate a `DbUpdateConcurrencyException` into a `ConflictException`.
- **Accepted crash windows** (a process death between the two commits) are recovered by the sweep; there is
  no compensating reversal by design — see [../../known-debt.md](../../known-debt.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
