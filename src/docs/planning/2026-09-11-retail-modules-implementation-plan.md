# Retail Modules — Implementation Plan

**Status:** Draft, pending user approval per root `CLAUDE.md` §2 rule 9 (code-change workflow gate). No code has been written yet — this is a design/planning artifact only.

**Scope:** Four new backend modules extending the Modular Monolith under `src/` — `Location`, `Catalog`, `Orders`, `Inventory` — rolled out in phases. `Purchasing` and `Transfers` are explicitly deferred (see §6).

**Design inputs:** produced by a `dotnet-architect` pass (strategic module boundaries, cross-module integration mechanism) and a `ddd-modeler` pass (tactical aggregates/invariants/value objects/domain events), both grounded in this repo's existing `Organization`/`Approval`/`LeaveManagement` conventions. No code was written during design.

---

## 0. Confirmed decisions

These were open policy questions raised during design and have been decided by the user — implementation should treat them as settled, not re-litigate them:

| Decision | Choice |
|---|---|
| Oversell / negative stock | **Strict — not allowed.** `StockLevel.Apply` throws if the result would go below zero. |
| Stock decrement trigger | **`Order.Place()`** (POS-style: stock leaves the moment the order is placed, not on a later "fulfilled" step). No `Reserved` quantity concept needed. |
| Tenant / Company scoping | **None for v1.** Location/Category/Product/Order uniqueness (Code, SKU, etc.) is global within its own module — no `CompanyId` scoping like `Organization` uses. |
| Payment deletion | **Void (soft), not hard delete.** Matches this repo's existing convention — no aggregate anywhere hard-deletes; `Payment.Void(reason)` preserves the audit trail. |

Carried-forward assumptions (not yet explicitly confirmed — flag before locking the schema if wrong):

- **Single currency for v1.** `Money` value object models a `Currency` field but a guard clause rejects anything but one configured default currency for now — cheap to relax later, not a schema change.
- **`Location` unifies Store/Warehouse/Terminal/Bin** under one self-referencing entity with a `LocationType` discriminator (same shape as `OrgUnit` unifying Department/Team) rather than four separate entities.
- **`Bin`'s parent is restricted to `Warehouse` only** (not `Store`). Revisit if small-format retail needs in-store bin locations.
- **`Cart` is not a separate concept** — it's an `Order` aggregate in `Draft` status.

---

## 1. Module map & build order

| Order | Module | Phase | Responsibility | Depends on (Contracts) |
|---|---|---|---|---|
| 1 | `Location` | 1 | Self-referencing physical-location hierarchy (Store/Warehouse/Terminal/Bin) — reused reference data, not Catalog-owned | — |
| 2 | `Catalog` | 1 | Categories (tree) + Products (VAT, images, SKU, price) | — |
| 3 | `Orders` | 1 | Cart/Order lifecycle, line-level price override + discount calc, order-level discount code, fees, Payments | `Location.Contracts`, `Catalog.Contracts` |
| 4 | `Inventory` | 2 | Stock adjustments, current stock level, reacts to placed orders | `Location.Contracts`, `Orders.Contracts` |
| — | `Purchasing`, `Transfers` | 3 (deferred) | Supplier POs / goods receipt; inter-location stock movement | `Location.Contracts`, `Inventory.Contracts` (not designed yet) |

`Location` and `Catalog` have no dependency on each other and can be built in either order (or in parallel by different sessions); `Orders` needs both to exist first; `Inventory` needs `Orders` to exist first (it subscribes to `Orders`' integration event).

**Why `Location` is its own module, not folded into `Catalog`:** three separate consumers need it (Catalog, Orders — which store/terminal a sale happened at, and Inventory — which location holds stock). Folding it into Catalog would force Inventory into a `Catalog.Contracts` reference just to resolve a Location it has no other interest in. Same shape as `Organization`'s `OrgUnit` + `IOrgDirectoryService` seam already proven in this repo.

**Shared-kernel touch flagged up front:** `Money` and `VatPercentage` value objects are reused by 2+ of these new modules (Catalog needs both; Orders needs both for line-item snapshots) and belong in `src/Shared`. Per root `CLAUDE.md` §2 rule 7, shared-kernel changes need explicit confirmation — this plan is that heads-up; it will be called out again as its own reviewable step when implementation reaches it (see §8, step 2).

---

## 2. Phase 1a — `Location` module

**Structure:** single-project, `src/Location.Api` + `src/Location.Contracts`, folders `Domain/`, `Data/`, `Application/Locations/{Commands,Queries}`, `Controllers/`. `LocationDbContext`, schema `location`.

**Domain — `Location` aggregate** (`AuditableEntity`, private ctor + `Create`/`Move` factory-style guarded methods, not public setters — real invariants live here):

- Fields: `Name`, `Code`, `LocationType Type` (`Store`/`Warehouse`/`Terminal`/`Bin`), `ParentLocationId?`, `Parent`/`Children` self-nav, `Status` (plain `LocationStatus` enum: `Active`/`Inactive` — not `Shared.ActiveStatus`, whose `Locked` state is User-specific).
- Invariants on the aggregate (`Create`/`Move`): allowed-parent-type table — `Terminal` requires a non-null parent of type `Store`; `Bin` requires a non-null parent of type `Warehouse`; `Store`/`Warehouse` must have no parent; leaf types (`Terminal`/`Bin`) cannot be given children.
- Invariant at handler level (needs a DB walk, not in-memory): no cycles — same pattern as `MoveOrgUnitCommandHandler.IsDescendantAsync` (walks `ParentId` one row at a time, no recursive CTE).
- `Code` uniqueness: handler pre-check + DB unique index (same belt-and-suspenders pattern as `Company.Code`/`OrgUnit.Code`).

**Contracts surface:** `LocationDto`, `LocationLookupDto` (thin: Id/Name/Code/Type), `LocationType` enum, `ILocationDirectoryService` (`GetAsync`, `ExistsAsync`, `GetChildrenAsync`, `GetLookupAsync`) — narrow, read-only, DI-only seam, same role as `IOrgDirectoryService`.

**API sketch:** CRUD endpoints for `Location` (create/update/list/get), a `Move` action endpoint (reparent), list-children.

**Migration:** `locations` table, self-referencing FK on `ParentLocationId`, unique index on `Code`.

**Tests to add later (sketch, not written yet):** entity-level invariant tests (invalid parent type per `LocationType`, self-parent rejection, leaf-type-with-children rejection), handler-level cycle-guard test, controller/integration CRUD tests — new `tests/Location.Tests` project mirroring the existing per-module test project pattern.

---

## 3. Phase 1b — `Catalog` module

**Structure:** single-project, `src/Catalog.Api` + `src/Catalog.Contracts`, folders `Domain/{Categories,Products}`, `Data/`, `Application/{Categories,Products}/{Commands,Queries}`, `Controllers/`. `CatalogDbContext`, schema `catalog`.

**Domain — `Category` aggregate:** self-referencing (`ParentCategoryId`), same shape as `Location`/`OrgUnit` but simpler (no type discriminator). Invariants: no cycles (handler-level walk, same as `Location.Move`), name unique among siblings (`(ParentCategoryId, Name)` composite index + handler pre-check). No max-depth guard for v1 (pragmatic — add later only if a concrete driver appears).

**Domain — `Product` aggregate:** private ctor + `Create` factory, guarded behavior methods (`Rename`, `UpdateDescription`, `Reprice`, `UpdateVatRate`, `AddImage`/`RemoveImage`, `Activate`/`Deactivate`, `Recategorize`) — deliberately the rich style (not Organization's plain-setter style), because VAT range / non-negative price / SKU format are real cross-field invariants.
- Value objects (destined for `src/Shared` where reused, module-local otherwise):
  - `VatPercentage` (`src/Shared`) — single `decimal Value`, guards `0–100`.
  - `Money` (`src/Shared`) — `Amount` + `Currency`, guards non-negative for price fields, single-currency guard clause for v1 (see §0).
  - `ProductImageUrl` (Catalog-local) — `Url` + optional `SortOrder`, a *list* on `Product`, not entities (no identity beyond "belongs to this product").
  - `Sku` (Catalog-local) — format-only guard (non-blank, trimmed, max length). Uniqueness is a repository-backed handler pre-check + DB unique index, same pattern as `Company.Code` — no new "domain service" abstraction invented.
- `CategoryId` is a real FK (same module as `Category`). `Status`: plain `ProductStatus` enum (`Active`/`Inactive`).

**Contracts surface:** `ProductDto`, `ProductPriceInfoDto` (Id, Price, Currency, VatRate — the lean lookup Orders needs), `CategoryDto`/`CategoryTreeNodeDto`, `ICatalogPricingService` (`GetPriceInfoAsync`, `GetPriceInfoBatchAsync`) — the seam `Orders` calls at line-add time to snapshot price/VAT. Catalog has no outgoing dependency on any other retail module.

**API sketch:** CRUD for `Category` (incl. tree/children query) and `Product` (incl. image add/remove, activate/deactivate).

**Migration:** `categories`, `products` tables; `products` gets an owned-type mapping for `Money`/`VatPercentage`/`Sku` (final owned-type shape left to `efcore-specialist` during implementation).

**Tests to add later (sketch):** `VatPercentage`/`Money`/`Sku` VO validation tests, `Product` factory/behavior-method guard tests, `Category` cycle/sibling-uniqueness tests, controller/integration tests — new `tests/Catalog.Tests` project.

---

## 4. Phase 1c — `Orders` module

**Structure:** single-project, `src/Orders.Api` + `src/Orders.Contracts`, folders `Domain/{Orders,Payments}`, `Data/`, `Application/{Orders,Payments}/{Commands,Queries}`, `Controllers/`. `OrdersDbContext`, schema `orders`.

**Domain — `Order` aggregate** (Draft status = the cart, no separate Cart type):
- Fields: `LocationId` (opaque FK into `Location` — **the store/terminal the sale happened at; required so `Inventory` knows which location's stock to decrement** — this was a gap in the initial tactical design and is added here explicitly), `MemberId?` (opaque, nullable FK — **no `Member` module exists yet; this is a forward-compatible reference slot only**, added at the user's request so a future Member/loyalty module can attach without an `Orders` schema change later. Nullable because guest/walk-in orders have no member. No behavior/validation tied to it now — just stored and returned on the DTO), `Status`, `_lines`/`_fees` field-backed collections exposed read-only, `Discount: OrderDiscount?`.
- Status state machine: `Draft → Placed → {PartiallyPaid, Paid} → Fulfilled`; `Cancelled` reachable from `Draft`/`Placed`/`PartiallyPaid` (not from `Paid`/`Fulfilled` — refund/return flow is future work, out of scope here).
- Guarded behavior methods: `AddLine`, `UpdateLineQuantity`, `SetLineSalePrice`, `RemoveLine`, `ApplyDiscount`/`RemoveDiscount`, `AddFee`/`RemoveFee` — all throw `ConflictException` unless `Status == Draft` (the single most important invariant, lives on the aggregate). `Place()` — requires ≥1 line, re-validates discount-≤-subtotal and total ≥ 0 as defense-in-depth, raises `OrderPlaced` domain event **and** is where the stock-decrement trigger point is anchored per §0. `Cancel()`, `MarkFulfilled()`, `ReconcilePaymentStatus(decimal totalPaid)` (idempotent — safe to call twice with the same value).
- `AddLine(productId, quantity, requestedSalePrice?)`: the **handler** resolves current price/VAT via `Catalog.Contracts`' `ICatalogPricingService` first (the aggregate never reaches into another module), then passes the resolved snapshot into `Order.AddLine(...)`.

**`OrderLine` child entity:** `ProductId` (opaque), snapshot `ProductName`/`Sku`/`UnitPrice: Money`/`VatRate: VatPercentage` captured once at `AddLine` time (never re-read from Product afterward — this is the point of snapshotting), `Quantity` (>0), `RequestedSalePrice: Money?`. `DiscountAmountPerUnit`/`DiscountPercentage` are **computed read-only properties**, not persisted columns (fully derivable from `UnitPrice`/`RequestedSalePrice`, avoids drift risk) — the API returns them in the same response as the add/update call, satisfying "auto-calculate and return to frontend" without a second query.

**`OrderDiscount` value object** (order-level code): `Kind` (`FixedAmount`/`Percentage`) + `Value`, with `ComputeAmount(subtotal)` behavior living on the VO itself (not a switch in `Order` or a handler) — matches this repo having no existing polymorphic VO hierarchy, keeps EF owned-type mapping simple. `efcore-specialist` to confirm the mapping during implementation.

**`OrderFee` child entity:** `Name`, `Amount: Money`, `Type` (plain enum `Shipping`/`Other`) — an entity (own `Id`, individually removable), not a VO list, same reasoning as `OrderLine`.

**`Payment` — its own aggregate** (own table, own repository), `OrderId` real FK (same module): `Create(orderId, amount, method, paidAt, reference?, recordedByUserId)`, `Void(reason, voidedAt)` (guarded — `ConflictException` if already voided; **no hard delete**, per §0). Sync with `Order`: **on-demand, same-handler, same-`SaveChangesAsync`** — the `RecordPayment`/`VoidPayment` handler creates/voids the `Payment`, sums non-voided payments for the order, and calls `order.ReconcilePaymentStatus(totalPaid)` in the same transaction. Deliberately *not* event-based — `Payment` and `Order` share one module/`DbContext`, so there's no boundary forcing eventual consistency, and using an event here would import the same non-atomic-dual-write risk this repo's `known-debt.md` already flags elsewhere as an accepted-but-regretted cost, with no reason to add it where it isn't required.

**Domain events (in-module):** `OrderPlaced`, `OrderCancelled`, `OrderFulfilled` — dispatched post-commit via `OrdersDbContext.SaveChangesAsync → IPublisher.DispatchDomainEvents(this)` (the `ApprovalDbContext` convention).

**Integration event (`Orders.Contracts`):** `OrderPlacedIntegrationEvent(OrderId, LocationId, IReadOnlyList<OrderLineSnapshot(OrderLineId, ProductId, Quantity)>) : INotification` — published by the command handler/service right after the `Place()` commit succeeds, wrapped so a faulting subscriber (Inventory) never fails the already-committed Place-Order response (same "best-effort, never fail the caller" pattern as `ApprovalService`'s integration-event publish).

**API sketch:** `Order` — create draft, add/update/remove line, apply/remove discount, add/remove fee, place, cancel, get, list (filter out `Draft` by default in list views). `Payment` — record, void, get, list-by-order.

**Migration:** `orders` (incl. nullable `MemberId` column, unindexed for now — add an index once a `Member` module exists and actual query patterns are known), `order_lines`, `order_fees`, `payments` tables.

**Tests to add later (sketch):** `Order` state-machine guard tests (mutation-after-Placed rejection, invalid transitions), `OrderLine` discount-computation tests, `OrderDiscount.ComputeAmount` tests, `Payment.Void` guard test, `ReconcilePaymentStatus` idempotency test, controller/integration tests — new `tests/Orders.Tests` project.

---

## 5. Phase 2 — `Inventory` module

**Structure:** single-project, `src/Inventory.Api` + `src/Inventory.Contracts`, folders `Domain/{StockAdjustments,StockLevels}`, `Data/`, `Application/...`, `Controllers/`. `InventoryDbContext`, schema `inventory`.

**Domain — `StockAdjustment` aggregate:** `Create(productId, locationId, quantityDelta, reason, occurredAt, performedByUserId, sourceOrderId?, sourceOrderLineId?)`. `ProductId`/`LocationId` opaque (cross-module, no FK). `Reason`: plain enum `ManualAdjustment`/`OrderFulfillment`, with unused reserved members `PurchaseReceipt`/`TransferIn`/`TransferOut` left in place for Phase 3 (cheap, additive future-proofing — no workflow built for them now). Guard: `quantityDelta != 0`.

**Idempotency (required, per §0's strict-no-oversell decision needing reliable delivery):** a persisted `IdempotencyKey` (`$"{SourceOrderId}:{SourceOrderLineId}"`, populated only for `Reason == OrderFulfillment`) with a **nullable unique index** — same NULL-handling shape already proven for `Employee.UserId` across all three configured EF providers. The event handler attempts the insert and treats a unique-violation as "already processed, no-op" (same idiom as `LinkEmployeeLoginCommandHandler`), never a hard error.

**Domain — `StockLevel` aggregate** (materialized, not sum-on-read): `ProductId` + `LocationId` (composite unique index), `QuantityOnHand`. `Apply(int delta)` is where the **strict no-negative-stock guard from §0 lives** — throws `ConflictException` (or similar) if the result would go below zero. Maintained in the **same handler/`SaveChangesAsync`** as the triggering `StockAdjustment` (intra-module, no event indirection needed) — this also means an oversold order attempt fails synchronously in the same call that's processing the `OrderPlacedIntegrationEvent`, which is what "strict" requires.

**Cross-module reaction:** `Inventory.Api` holds `INotificationHandler<OrderPlacedIntegrationEvent>`, referencing only `Orders.Contracts`. For each line snapshot in the event: build the idempotency key, `StockAdjustment.Create(...)` + `stockLevel.Apply(-quantity)` in one handler/transaction, catch unique-violation as no-op. **Recommended addition, not yet explicitly scoped by the user:** a periodic reconciliation `BackgroundService` (mirrors `LeaveRequestReconciliationService`) that finds `Placed` orders with no corresponding stock movement after N minutes and reprocesses them — this is the safety net for best-effort in-process event delivery. Flagged as an open scope question in §10, not assumed into the plan.

**Contracts surface:** `StockLevelDto`, `RecordStockMovementRequest` (for the manual-adjustment API), `StockMovementType` enum (same reserved-values approach as `Reason` above).

**API sketch:** manual stock adjustment (create), stock level query (by product/location), stock movement history/list.

**Migration:** `stock_adjustments` (with nullable unique index on `IdempotencyKey`), `stock_levels` (composite unique index on `(ProductId, LocationId)`).

**Tests to add later (sketch):** `StockLevel.Apply` negative-guard test, `StockAdjustment` idempotency/duplicate-delivery test, the `OrderPlacedIntegrationEvent` handler's no-op-on-retry behavior, controller/integration tests — new `tests/Inventory.Tests` project.

---

## 6. Deferred — Purchasing & Transfers

Not designed in this pass. `Inventory`'s `StockMovementType`/`Reason` enums intentionally reserve unused values (`PurchaseReceipt`, `TransferIn`, `TransferOut`) so these modules can write to the same `StockAdjustment` ledger later without a schema change — no further speculative building now (YAGNI). When prioritized, route through `dotnet-architect` (module boundary — likely two more modules, `Purchasing` and `Transfers`, each with their own `.Contracts`) and `ddd-modeler` (aggregates: `PurchaseOrder`/`GoodsReceipt` for Purchasing, `StockTransfer` for Transfers) as a fresh design pass, same as this one.

---

## 7. Cross-cutting notes

- **Composition root:** `StarterKit.WebApi` needs each new module's `DbContext` + module registration wired in, following the existing module bootstrap pattern (see how `Approval`/`LeaveManagement` are currently registered).
- **Shared kernel:** adding `Money` + `VatPercentage` to `src/Shared` — flagged in §1 as a wide-blast-radius change per root `CLAUDE.md` §2 rule 7; will be its own explicitly-called-out step (§8 step 2), not silently bundled into Catalog's implementation.
- **Migrations:** per this project's established workflow, add incremental MSSQL-only migrations per schema change during development; a full from-scratch regenerate (MSSQL and/or other providers) happens only on explicit user request once a module is complete.
- **EF Core mapping specifics** (owned-type shapes for `Money`/`VatPercentage`/`OrderDiscount`, indexes, concurrency tokens) are intentionally left to `efcore-specialist` at implementation time, not finalized here.
- **API contract specifics** (exact routes, versioning, request/response DTO shapes, error contract) are intentionally left to `api-designer` at implementation time, not finalized here.

---

## 8. Recommended implementation sequencing

Each numbered step is its own `implement-feature` pass — plan (this doc covers the design; a final per-step plan confirmation happens before that step's code is written), implement via `dotnet-developer`, present code for review, then tests/docs only on a separate explicit follow-up request. Recommend approving and shipping **one module at a time** rather than all four at once, to keep review batches reviewable.

1. **`Location` module** — domain, persistence, API, Contracts. No dependencies.
2. **Shared kernel** — `Money`, `VatPercentage` value objects in `src/Shared`. Called out as its own reviewable step since it touches shared/building-blocks code.
3. **`Catalog` module** — `Category`, `Product`. Depends on step 2 for the VOs.
4. **`Orders` module** — `Order`/`OrderLine`/`OrderDiscount`/`OrderFee`, `Payment`. Depends on steps 1 and 3 (`Location.Contracts`, `Catalog.Contracts`).
5. **`Inventory` module** — `StockAdjustment`, `StockLevel`, the `OrderPlacedIntegrationEvent` subscriber. Depends on steps 1 and 4.
6. *(Deferred)* `Purchasing`, `Transfers` — fresh design pass when prioritized.

---

## 9. Open items still flagged (not blocking, but worth a decision before the relevant step)

1. **Inventory reconciliation `BackgroundService`** (§5) — recommended as the delivery backstop for best-effort event processing, but not yet explicitly scoped in/out by the user. Confirm before or during step 5.
2. **`OrderDiscount` mapping** — modeled as one VO with a `Kind` discriminator rather than two polymorphic subtypes; `efcore-specialist` to confirm this maps cleanly as an EF owned type during step 4, or that the polymorphic alternative is worth the extra complexity.
3. **Currency** — single-currency guard clause assumed for v1 (§0); revisit if true multi-currency is actually needed, since that also implies an FX-rate concept for order totals.
4. **`Bin` parent restricted to `Warehouse`** (§0) — revisit if in-store bin locations under a `Store` turn out to be needed.

---
_This plan is a snapshot of the design decisions made on 2026-09-11. It is not synced automatically — if scope changes during implementation, update this file or note the deviation in the relevant step's own plan._
