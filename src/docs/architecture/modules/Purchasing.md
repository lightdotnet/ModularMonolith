# Module Overview: Purchasing

## Purpose

Owns buying goods from suppliers into one receiving location: a `Supplier` entity plus three document
aggregates sharing the module's one `PurchasingDbContext`.

- **`Supplier`** — a plain entity (code, name, contact/payment-terms fields, `Active`/`Inactive`); only an
  active supplier can go on a new purchase order. The code is normalized (trimmed, upper-cased) and unique.
- **`PurchaseOrder`** (aggregate root, `PurchaseOrderLine` children) — `Draft` → `PendingApproval` →
  `Approved` / `Rejected`; `Approved`/`PartiallyReceived` → `PartiallyReceived`/`Received`;
  `PartiallyReceived` → `Closed`; `Draft`/`Rejected`/`Approved` (while nothing has been received) →
  `Cancelled`. `Draft` and `Rejected` are the only editable states. Approval goes through the `Approval`
  module exactly like `LeaveManagement` (see Notable Conventions and [LeaveManagement.md](LeaveManagement.md)).
- **`GoodsReceipt`** (`GoodsReceiptLine` children) — one delivery against an approved order. **Immutable**
  once recorded: no edit, no cancel (a mistaken receipt is corrected with a purchase return). It is recorded
  `Posting`, becomes `Posted` once Inventory confirms the inbound stock (`ReceiveStockAsync`), and ends
  `Voided` if Inventory deterministically refuses the posting. Each line freezes the order line's unit cost
  at receipt time — the cost the stock enters Inventory at.
- **`PurchaseReturn`** (`PurchaseReturnLine` children) — goods sent back against one posted goods receipt:
  `Draft` → `Posting` → `Posted` → `Credited`, with `Cancelled` from `Draft`. Stock leaves through
  `IssueStockAsync` at Inventory's current moving average; a refused issue returns the return to `Draft`. A
  posted return is never reversed. The supplier's credit note is recorded as informational bookkeeping only
  (`Credited` gates nothing). Over-return is prevented by a per-receipt-line quantity rule across all
  non-cancelled returns; returns also update informational returned quantities on the order without ever
  reopening it.

Purchase costs are `Money` in `CurrencyConstants.Default`: the buyer supplies the unit cost on each order
line, and the module hard-codes that currency (`PurchaseOrder`/`PurchaseOrderLine` and the add/update-line
handlers) rather than reading the base currency from the `Currency` module — the module has no dependency on
`Currency`. This only matches Inventory's base-currency valuation while the seeded base is that currency
(see [../../known-debt.md](../../known-debt.md)). Names, locations, and the supplier on documents are
snapshots. The supplier is a real same-module FK; the location is an opaque `Location` id.

## Internal Layering

Purchasing is a **single-project module** (not split Domain/Application/Infrastructure/Api), the same
structural convention as `Location`/`Catalog`/`Orders`/`Inventory`/`Transfers`:

| Project | Responsibility | Notes |
|---|---|---|
| `Purchasing.Contracts` | DTOs, requests, status/reason enums, limits, and the permission catalog, in per-feature subfolders (`Common/`, `Suppliers/`, `PurchaseOrders/`, `GoodsReceipts/`, `PurchaseReturns/`, `Authorization/`). Each request carries its own `AbstractValidator` in the same file. Declares `Lightsoft.AspNetCore.Authorization` directly; references `Shared`. Exposes no cross-module seam and no events. |
| `Purchasing.Api` | Single project organized by folder: `Domain/{Suppliers,PurchaseOrders,GoodsReceipts,PurchaseReturns}/` — the aggregates (private ctors; factories; every state change and quantity rule on the aggregate), their document-number value objects, specs, and domain events (dispatched, no handler yet). `Data/` (`PurchasingDbContext`, `PurchasingContextInitialiser` — migrates only, no seed data). `Services/` (`ReceivingLocationResolver`). `Application/{Suppliers,PurchaseOrders,GoodsReceipts,PurchaseReturns}/{Commands,Queries}` — handlers own their `PurchasingDbContext` logic directly, plus a thin per-command `AbstractValidator`; `Application/PurchaseOrders/` also holds the `PurchaseOrderApprovalCoordinator`, the `ApprovalFinalizedIntegrationEventHandler`, and the `PurchaseOrderApprovalReconciliationService`; `Application/Posting/` holds the shared `PurchasingPosting` orchestration, `PurchasingSaving`, and the `PurchasingPostingReconciliationService`. `Controllers/` (`SupplierController`, `PurchaseOrderController`, `GoodsReceiptController`, `PurchaseReturnController`, `PurchasingAccess`). `PurchasingModule.cs` (DI: DbContext, resolver, coordinator, permission provider, both reconciliation option sets + hosted services). |

## Public Contract

All controllers are versioned (`api/v{version}/…`), lower-case routes, `[MustHavePermission]` at class level
on the `view` permission.

`SupplierController` (route `supplier`; class-level `purchasing.suppliers.view`): `GET` (search), `GET {id}`,
and — each `purchasing.suppliers.manage` — `POST`, `PUT {id}`, `PUT {id}/activate`, `PUT {id}/deactivate`.

`PurchaseOrderController` (route `purchase_order`; class-level `purchasing.orders.view`):

| Route | Verb | Permission | Notes |
|---|---|---|---|
| `purchase_order` | GET | `orders.view` | `SearchPurchaseOrderRequest` (`Status?`, `SupplierId?`, `LocationId?`, paging) |
| `purchase_order/{id}` | GET | `orders.view` | Order with lines |
| `purchase_order/approvers` | GET | `orders.submit` | The caller's approver candidates, resolved through Organization |
| `purchase_order` | POST | `orders.create` | `CreatePurchaseOrderRequest { SupplierId, LocationId, ExpectedAt?, Note? }`; the requester is the caller (user id and linked employee id) |
| `purchase_order/{id}` | PUT | `orders.create` | Header edit (`Draft`/`Rejected`) |
| `purchase_order/{id}/line` | POST | `orders.create` | Add a line (product existence checked through `ICatalogPricingService`; buyer supplies the unit cost) |
| `purchase_order/{id}/line/{lineId}` | PUT / DELETE | `orders.create` | Edit/remove a line |
| `purchase_order/{id}/submit` | PUT | `orders.submit` | `SubmitPurchaseOrderRequest { ApproverEmployeeId }`; requester only |
| `purchase_order/{id}/withdraw` | PUT | `orders.submit` | Requester only; back to `Draft` |
| `purchase_order/{id}/receipt` | POST | `receipts.create` | `ReceivePurchaseOrderRequest { DeliveryNoteRef?, ReceivedAt, Lines[{ PurchaseOrderLineId, Quantity }] }` → receipt id |
| `purchase_order/{id}/close` | PUT | `orders.close` | `{ Reason }`; only a `PartiallyReceived` order; gives up the remainder without posting stock |
| `purchase_order/{id}/cancel` | PUT | `orders.create` | `{ Reason }`; see the owner rule below |

There is **deliberately no approve/decide endpoint and no approve permission**: the order is decided by the
approver chosen at submission, on the `Approval` module's own decision surface, and the outcome flows back.

`GoodsReceiptController` (route `goods_receipt`; class-level `purchasing.receipts.view`): read-only —
`GET` (search by `Status?`, `PurchaseOrderId?`, `SupplierId?`, `LocationId?`) and `GET {id}`. Receiving goes
through `purchase_order/{id}/receipt`.

`PurchaseReturnController` (route `purchase_return`; class-level `purchasing.returns.view`): `GET` (search),
`GET {id}`; `returns.create` — `POST` (`CreatePurchaseReturnRequest { GoodsReceiptId, Reason, Note?, Lines }`),
`PUT {id}` (edit a draft), `PUT {id}/post`, `PUT {id}/cancel`; `returns.credit` — `PUT {id}/credit`
(`{ CreditNoteNumber, CreditAmount }`, only a `Posted` return). `CostRemovedBase` is null unless the caller
has Inventory's `view_cost`.

Permissions (`PurchasingPermissions`): `Suppliers.{View,Manage}`, `Orders.{View,Create,Submit,Close}`,
`Receipts.{View,Create}`, `Returns.{View,Create,Credit}` — per-action, not the four-way CRUD split
(`Orders.Create` also covers editing and cancelling; `Returns.Create` covers create/edit/post/cancel).

## Data Access

`PurchasingDbContext : BaseDbContext`, schema `"purchasing"`, registered via
`AddConfiguredDbContext<PurchasingDbContext>(configuration, DbConnectionNames.Purchasing)`;
`DbConnectionNames.Purchasing` aliases `Default` — the same physical database as every other module,
separated by schema + table name.

Seven tables, all `bigint IDENTITY(1,1)` keys (`AuditableEntity<long>`): `Suppliers`, `PurchaseOrders`,
`PurchaseOrderLines`, `GoodsReceipts`, `GoodsReceiptLines`, `PurchaseReturns`, `PurchaseReturnLines`.
Notable configuration:

- Document numbers (`PONumber`, `ReceiptNumber`, `ReturnNumber`) are `HasConversion` scalars with unique
  indexes, not owned types (same treatment as `Orders`' `OrderCode`); `Suppliers.Code` is unique.
- `PurchaseOrders` → `Suppliers`, `GoodsReceipts` → `PurchaseOrders`, `PurchaseReturns` → `GoodsReceipts`
  are real FKs with `Restrict`; lines cascade from their parent with field-backed navigations.
- `GoodsReceipts` — unique `(PurchaseOrderId, DeliveryNoteRef)` filtered to receipts that carry a
  reference and are not voided (a voided receipt's reference can be submitted again). Filtered indexes are
  written with `HasProviderFilter` so the filter text is right per provider.
- Money columns are `decimal(19,4)` (`UnitCost`, `UnitCostBase`, `ReceiptUnitCostBase`, `CostRemovedBase`,
  `ExpectedCreditBase`, `CreditAmountBase`).
- Indexes serving the sweeps: `(Status, Created)` on orders, receipts, and returns, and a filtered
  `PostingStartedAt` index on returns.
- Each aggregate root has an app-managed `ConcurrencyToken`. `RotateConcurrencyTokens()` rotates a root's
  token when it **or a dependent document** changes: an order when its lines change or a receipt is recorded
  against it (so two racing receipts collide before either posts stock); a receipt when a return against it
  changes (so two racing returns cannot both pass the over-return guard); a return when its lines change (so
  the original request and the sweep cannot both finish it).

`SaveChangesAsync` audits, rotates tokens, and dispatches domain events post-commit (try/catch-log); the
synchronous `SaveChanges` stays audit + rotation only. The aggregates raise events, but no handler in any
module subscribes to them. No entity implements `ISoftDelete`.

`PurchasingContextInitialiser.InitialiseAsync()` applies migrations only; each provider's migrator
`Program.cs` calls it. Migrations: see [../../conventions/migrations.md](../../conventions/migrations.md).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Purchasing.Contracts → Shared`) | `BaseDto<long>`, `AuditableEntity<long>`, `Money`, `CurrencyConstants`, `ICurrentUser`/`IDateTime`, permission and claim helpers (`GetEmployeeId`, `HasPermission`, `IsFullControl`). |
| `Infrastructure` | project (`Purchasing.Api → Infrastructure`) | `VersionedApiController`, `AppModule`. |
| `Persistence` | project (`Purchasing.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, audit tracking, domain-event dispatch, `HasProviderFilter`, paging, `IsUniqueConstraintViolation`. |
| `Approval.Contracts` | project (`Purchasing.Api → Approval.Contracts`) | `IApprovalService` (`CreateAsync`/`CancelAsync`, and the by-stored-id `GetStatusAsync`/`GetStatusesAsync` lookups), `ApprovalRequestTypes.PurchaseOrder`, and `ApprovalFinalizedIntegrationEvent`. |
| `Organization.Contracts` | project (`Purchasing.Api → Organization.Contracts`) | `IOrgDirectoryService` — approver candidates and the requester's display name. |
| `Inventory.Contracts` | project (`Purchasing.Api → Inventory.Contracts`) | `IInventoryService` (`ReceiveStockAsync`, `IssueStockAsync`, `FilterSourceIdsWithUnreversedPostingsAsync`), `InsufficientStockException`, and `InventoryPermissions.Stock.ViewCost` for cost masking. |
| `Location.Contracts` | project (`Purchasing.Api → Location.Contracts`) | `ILocationDirectoryService`, through `ReceivingLocationResolver`. |
| `Catalog.Contracts` | project (`Purchasing.Api → Catalog.Contracts`) | `ICatalogPricingService`, for the product name/SKU snapshot on a new line (existence only; the catalog sell price is irrelevant to a purchase cost). |
| `Purchasing.Contracts` | project (`Purchasing.Api → Purchasing.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result` | package, all declared directly | No undeclared-transitive-dependency instance. |

## Depended On By

- `StarterKit.WebApi` — composition-root host (`ConfigureExtensions.cs`'s `assemblies` array).
- `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` — each references `Purchasing.Api` for the `DbContext`/initialiser.
- `Purchasing.Tests` — `Purchasing.Api.csproj` grants `InternalsVisibleTo` for the test project plus
  `DynamicProxyGenAssembly2` for Moq.

No business module references `Purchasing.Api`/`Purchasing.Contracts`.

## Notable Conventions

- **Approval works exactly like `LeaveManagement`'s**, via a `PurchaseOrderApprovalCoordinator` that is the
  single seam for the cross-module choreography. Submit is **requester-only**: the order's own guard runs
  first (so a submission that cannot succeed never leaves a workflow behind), the chosen approver is
  validated against the requester's Organization approver candidates, a **single-step** `Approval` workflow
  is created *before* any local change (a resubmission after a rejection always creates a brand-new one), and
  one local commit records the workflow id; if that commit fails the just-created workflow is cancelled
  best-effort. Withdraw is requester-only and clears the workflow id. `Approval` itself only lets the
  step's assigned approver decide. The outcome returns through the `ApprovalFinalizedIntegrationEvent`
  handler, backed by `PurchaseOrderApprovalReconciliationService` (options
  `Purchasing:ApprovalReconciliation`) which sweeps orders still `PendingApproval` and pulls their status
  from `Approval` by the order's stored approval request id (`GetStatusesAsync`; withdraw uses
  `GetStatusAsync`), so a request forged against the same source record cannot shadow the real workflow.
  `PurchaseOrder.ApplyApprovalOutcome` is the single choke point: it ignores a decision for
  a superseded workflow id or an order no longer pending, so late or repeated deliveries are harmless.
  Approval display labels are snapshots (see [../../known-debt.md](../../known-debt.md) D5).
- **Owner rule for edit and cancel.** Editing the header or lines, and cancelling a `Draft`/`Rejected`
  order, is for the requester **or a holder of `purchasing.orders.close`** (or full control — a "purchasing
  manager"); anyone else is refused (403). Cancelling an `Approved` order (allowed only while nothing has
  been received, including a receipt still posting) needs `orders.close` regardless of who requested it. A
  `PendingApproval` order must be withdrawn first. Submit and withdraw stay requester-only.
- **Posting-first, two-commit sequences (no shared transaction with Inventory)** for goods receipts and
  purchase returns, the same shape as `Transfers` (see [Transfers.md](Transfers.md)) — the document is
  committed as `Posting` first so it has an id to post under, then Inventory is called, then a second commit
  records the outcome (a receipt is marked `Posted` **and** its quantities applied to the order in one
  save; a return records the cost removed per line, the frozen expected credit, and the informational
  returned quantities). The shared `PurchasingPosting` class serves both the handlers and the sweep; every
  Inventory call is idempotent per line. After a failed post Inventory is asked whether anything landed:
  if so the document is rolled forward, only a deterministic refusal with nothing landed voids a receipt or
  aborts a return to `Draft`, and a transient failure leaves it in `Posting`. Once stock has landed,
  commit 2 is retried against freshly loaded state if a concurrent write rotated a token; if that keeps
  failing a conflict saying the movement was recorded is thrown and the sweep finishes it. **Nothing posted
  to Inventory is ever reversed.** Movements are attributed to the user recorded on the document
  (`ReceivedByUserId`/`PostedBy`), also when the sweep finishes them.
- **`PurchasingPostingReconciliationService`** finishes receipts and returns stuck in `Posting`: options
  `Purchasing:PostingReconciliation` (validated on start, every default in code — enabled, interval,
  minimum time in `Posting` before pickup, batch size), fresh DI scope per tick, keyset paging with
  in-memory watermarks; receipts whose postings already landed are applied without calling Inventory again;
  a document that stays `Posting` for many multiples of the grace period is logged at Error.
- **Receipt idempotency is the supplier delivery-note reference.** A repeated `DeliveryNoteRef` resolves to
  the receipt already recorded (including a concurrent duplicate that loses the insert race) provided the
  requested lines are identical, otherwise it is a conflict; a receipt still `Posting` is finished under its
  original recorder. A lost race with a *different* delivery is retried against fresh state (bounded), so it
  either succeeds or is rejected by the quantity guard. A delivery cannot be dated before the order's
  approval or (with a small clock-skew tolerance) in the future.
- **Cost masking is limited to what Inventory owns.** `CostRemovedBase` on a purchase return is Inventory's
  moving-average cost, so it is null unless the caller has `inventory.stock.view_cost` (or full control),
  decided by `PurchasingAccess.CanViewStockCost`. Purchasing's own purchase prices are not masked.
- **Validation split**: input-shape checks (required, length, ranges, future-date bound) live in
  FluentValidation on the request/command; state and quantity rules live on the aggregates.
- **Accepted crash windows** (a process death between the two commits) are recovered by the sweeps — see
  [../../known-debt.md](../../known-debt.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
