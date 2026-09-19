# Module Overview: Orders

## Purpose

Owns the sale lifecycle — from an editable draft cart through placement, payment reconciliation,
fulfillment or cancellation — via two aggregates that share the module's one `OrdersDbContext`/database
but stay deliberately decoupled from each other. `Order` is the aggregate root: a `LocationId` (an
opaque reference into `Location`, validated but not FK-constrained across the module boundary), an
optional forward-compat `MemberId`, a unique human-readable `OrderCode` (generated or
caller-supplied — see Notable Conventions), an optional `ExternalReferenceCode` pass-through to an
external system (POS/marketplace), a `Status` state machine (`Draft` → `Placed` →
`PartiallyPaid`/`Paid` → `Fulfilled`, or `Draft`/`Placed`/`PartiallyPaid` → `Cancelled`), and an
app-managed `ConcurrencyToken`. `OrderLine` and `OrderFee` are child entities of `Order` — own `Id`,
individually add/removable while the order is still a draft, each carrying a denormalized `OrderCode`
string snapshot of the parent so a row from either table is self-describing without a join back to
`Orders`. `Payment` is a **separate aggregate root**, sharing the database but holding only a loose,
unconstrained `OrderId` (no navigation, no FK) plus its own `OrderCode` snapshot — recording or voiding
a payment never loads or locks the order graph.

A third, standalone entity, `OrderType`, is a data-driven catalog of fee types and payment types
(replacing what would otherwise be hardcoded enums). It is administered independently of any order;
`OrderFee` and `Payment` reference it by plain string id plus a name snapshot, never by FK (see Notable
Conventions).

`Order` has no idea what a product actually costs or whether a location exists: `AddOrderLineCommandHandler`
resolves current pricing via `Catalog.Contracts.ICatalogPricingService.GetPriceInfoAsync` and snapshots
name/price/VAT rate/SKU onto the new `OrderLine`, and `CreateOrderCommandHandler` checks
`Location.Contracts.ILocationDirectoryService.ExistsAsync` before creating the order — Orders is the
first (and, so far, only) consumer of either cross-module seam. Placing an order publishes
`Orders.Contracts.Events.OrderPlacedIntegrationEvent`, a best-effort in-process notification a future
module (e.g. Inventory) can subscribe to.

## Internal Layering

Orders is a **single-project module** (not split Domain/Application/Infrastructure/Api), following the
same structural convention as `Organization`/`Approval`/`LeaveManagement`/`Location`/`Catalog`:

| Project | Responsibility | Notes |
|---|---|---|
| `Orders.Contracts` | DTOs, requests, enums, and the permission catalog, organized into per-feature subfolders — `Common/` (`OrderStatus`: `Draft`/`Placed`/`PartiallyPaid`/`Paid`/`Fulfilled`/`Cancelled`; `OrderDiscountKind`: `FixedAmount`/`Percentage`; `OrderTypeCategory`: `Fee`/`Payment`; `OrderTypeStatus`: `Active`/`Inactive`), `Orders/` (`OrderDto` (flattens `Order`'s computed `Subtotal`/`DiscountAmount`/`FeesTotal`/`Total` to plain `decimal`s, never a nested value-object shape; includes `IList<OrderLineDto> Lines`/`IList<OrderFeeDto> Fees`), `OrderLineDto`, `OrderFeeDto`, `CreateOrderRequest`, `AddOrderLineRequest`, `UpdateOrderLineQuantityRequest`, `SetOrderLineSalePriceRequest`, `ApplyOrderDiscountRequest`, `AddOrderFeeRequest`, `CancelOrderRequest`, `SearchOrderRequest : SearchQuery`), `Payments/` (`PaymentDto`, `RecordPaymentRequest`, `VoidPaymentRequest`), `OrderTypes/` (`OrderTypeDto`, `CreateOrderTypeRequest`, `UpdateOrderTypeRequest`), `Events/` (`OrderPlacedIntegrationEvent(OrderId, LocationId, PlacedAt, IReadOnlyList<OrderLineSnapshot> Lines) : INotification`, `OrderLineSnapshot`), `Authorization/` (`OrdersPermissions`, `OrdersPermissionProvider`). Every Request record carries its own `AbstractValidator<TRequest>` **in the same file** — the same two-layer FluentValidation convention `Location` established. Declares `Lightsoft.AspNetCore.Authorization`, `Lightsoft.Mediator` (for `INotification`) directly; `GlobalUsings.cs` globals `StarterKit.Shared`. Also carries `ProjectReference`s to `Catalog.Contracts` and `Location.Contracts` (see Dependencies — currently unused by any type in this project; every cross-module-looking field here, e.g. `OrderDto.LocationId`, is a plain `string`). |
| `Orders.Api` | Single project organized by folder: `Domain/Orders/` — the `Order` aggregate (private ctor; `Create` factory; draft-only line/fee/discount editors; `Place`/`Cancel`/`MarkFulfilled`/`ReconcilePaymentStatus`; internal `RotateConcurrencyToken`/`RegenerateOrderCode`), its `OrderCode` value object, its `OrderLine`/`OrderFee` children, the optional owned `OrderDiscount` value object, `OrderByIdSpec`, and three domain events `OrderPlacedEvent`/`OrderCancelledEvent`/`OrderFulfilledEvent` (all `internal sealed record : DomainEvent`, see Notable Conventions — no handler subscribes to any of them yet). `Domain/Payments/` — the separate `Payment` aggregate + `PaymentByIdSpec`. `Domain/OrderTypes/` — the `OrderType` catalog entity + `OrderTypeByIdSpec`. `Order`/`OrderLine`/`OrderFee`/`Payment` are `: AuditableEntity<long>`; `OrderType` is string-keyed (see Notable Conventions). `Data/` (`OrdersDbContext`, `OrdersContextInitialiser`). `Services/` (`IOrderTypeCache`/`OrderTypeCache`). `Application/Orders/{Commands,Queries}` — `CreateOrder`, `AddOrderLine`, `UpdateOrderLineQuantity`, `SetOrderLineSalePrice`, `RemoveOrderLine`, `ApplyOrderDiscount`, `RemoveOrderDiscount`, `AddOrderFee`, `RemoveOrderFee`, `PlaceOrder`, `CancelOrder`, `MarkOrderFulfilled`; `GetOrderById`, `SearchOrders` — every handler owns its `OrdersDbContext` logic directly, no service-class indirection, plus a thin per-command `AbstractValidator`. `Application/Payments/{Commands,Queries}` — `RecordPayment`, `VoidPayment`; `GetPaymentsByOrder`. `Application/OrderTypes/{Commands,Queries}` — `CreateOrderType`, `UpdateOrderType`, `DeleteOrderType`; `GetOrderTypes`, `GetOrderTypeById`. `Controllers/` (`OrderController`, `PaymentController`, `OrderTypeController`). `OrdersModule.cs` (DI: DbContext, `IOrderTypeCache`, permission provider — unlike `Catalog`/`Location`, `Orders` exposes no cross-module seam of its own, only consumes others'). |

## Public Contract

`OrderController` (route `order`, `[MustHavePermission(OrdersPermissions.Orders.View)]` at class
level; every route id is `long`):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/order` | GET | `orders.orders.view` | `SearchOrderRequest` (`LocationId?`, `Status?`, `SearchQuery` paging) | `PagedResult<OrderDto>`, ordered by `Created` desc; a `Draft` order is hidden from the default list (still an editable cart, not a real sale) unless `Status=Draft` is explicitly requested |
| `api/v{version}/order/{id}` | GET | `orders.orders.view` | Route `id` | `Result<OrderDto>`, includes `Lines`/`Fees` |
| `api/v{version}/order` | POST | `orders.orders.manage` | `CreateOrderRequest { LocationId, MemberId?, OrderCode?, ExternalReferenceCode? }` | `Result<long>` (new id); validates `LocationId` via `ILocationDirectoryService.ExistsAsync`, then `Order.Create` — a supplied `OrderCode` is pre-checked/conflict-on-collision, an omitted one is generated with a bounded retry loop (see Notable Conventions) |
| `api/v{version}/order/{id}/line` | POST | `orders.orders.manage` | `AddOrderLineRequest { ProductId, Quantity, RequestedSalePrice? }` | `Result`; resolves pricing via `ICatalogPricingService.GetPriceInfoAsync`, 404s if the product is missing or not `Active`, then `Order.AddLine` (draft-only) |
| `api/v{version}/order/{id}/line/{lineId}/quantity` | PUT | `orders.orders.manage` | `UpdateOrderLineQuantityRequest { Quantity }` | `Result`; `Order.UpdateLineQuantity` (draft-only) |
| `api/v{version}/order/{id}/line/{lineId}/sale_price` | PUT | `orders.orders.manage` | `SetOrderLineSalePriceRequest { SalePrice? }` | `Result`; `Order.SetLineSalePrice` — `null` clears the override, a value cannot exceed the line's `UnitPrice` (draft-only) |
| `api/v{version}/order/{id}/line/{lineId}` | DELETE | `orders.orders.manage` | Route ids | `Result`; `Order.RemoveLine` (draft-only) |
| `api/v{version}/order/{id}/discount` | PUT | `orders.orders.manage` | `ApplyOrderDiscountRequest { Kind, Value }` | `Result`; `Order.ApplyDiscount` — replaces any existing discount wholesale (draft-only) |
| `api/v{version}/order/{id}/discount` | DELETE | `orders.orders.manage` | Route `id` | `Result`; `Order.RemoveDiscount` — no-op if there is none (draft-only) |
| `api/v{version}/order/{id}/fee` | POST | `orders.orders.manage` | `AddOrderFeeRequest { Name, Amount, FeeTypeId }` | `Result<long>` (new fee id); the `FeeTypeId` must resolve to an `Active` `Fee`-category `OrderType` (looked up via `IOrderTypeCache`), whose name is snapshotted onto the fee; then `Order.AddFee` (draft-only) |
| `api/v{version}/order/{id}/fee/{feeId}` | DELETE | `orders.orders.manage` | Route ids | `Result`; `Order.RemoveFee` (draft-only) |
| `api/v{version}/order/{id}/place` | PUT | `orders.orders.manage` | Route `id` | `Result`; `Order.Place` — requires at least one line, re-validates discount-vs-subtotal and non-negative total, flips `Draft` → `Placed`, queues `OrderPlacedEvent`, then best-effort publishes `OrderPlacedIntegrationEvent` after the commit |
| `api/v{version}/order/{id}/cancel` | PUT | `orders.orders.manage` | `CancelOrderRequest { Reason }` | `Result`; `Order.Cancel` — only from `Draft`/`Placed`/`PartiallyPaid`, requires a reason, queues `OrderCancelledEvent` |
| `api/v{version}/order/{id}/fulfill` | PUT | `orders.orders.manage` | Route `id` | `Result`; `Order.MarkFulfilled` — only from `Paid`, queues `OrderFulfilledEvent` |

`PaymentController` (route `payment`, `[MustHavePermission(OrdersPermissions.Payments.View)]` at class
level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/payment/order/{orderId}` | GET | `orders.payments.view` | Route `orderId` | `IReadOnlyList<PaymentDto>`, ordered by `Created` desc |
| `api/v{version}/payment/order/{orderId}` | POST | `orders.payments.manage` | `RecordPaymentRequest { Amount, Currency, PaymentTypeId, PaidAt, Reference? }` | `Result<long>` (new payment id); the `PaymentTypeId` must resolve to an `Active` `Payment`-category `OrderType` (name snapshotted onto the payment), then `Payment.Create`, then `Order.ReconcilePaymentStatus` recomputes `AmountPaid`/`Status` from the sum of all non-voided payments including this new one |
| `api/v{version}/payment/{id}/void` | PUT | `orders.payments.manage` | `VoidPaymentRequest { Reason }` | `Result`; `Payment.Void` (does not delete — the audit trail of who recorded/voided and why is kept), then `Order.ReconcilePaymentStatus` recomputes from the remaining non-voided sum |

`OrderTypeController` (route `order_type`, `[MustHavePermission(OrdersPermissions.OrderTypes.View)]` at
class level; a composite `{category}/{id}` address because `Id` is only unique within a category):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/order_type` | GET | `orders.order_types.view` | Optional `?category=` (`Fee`/`Payment`) | All order types, or only those of one category |
| `api/v{version}/order_type/{category}/{id}` | GET | `orders.order_types.view` | Route `category`, `id` | `Result<OrderTypeDto>` |
| `api/v{version}/order_type` | POST | `orders.order_types.manage` | `CreateOrderTypeRequest` (carries the caller-supplied `Id` code, `Category`, `Name`) | `Result`; category and id are immutable after creation |
| `api/v{version}/order_type/{category}/{id}` | PUT | `orders.order_types.manage` | `UpdateOrderTypeRequest` | `Result`; updates the mutable fields (name, status) only |
| `api/v{version}/order_type/{category}/{id}` | DELETE | `orders.order_types.manage` | Route `category`, `id` | `Result`; unconditional — there is no in-use guard, since orders keep their own id + name snapshot |

Every action across the three controllers dispatches a mediator command/query under
`Application/{Orders,Payments,OrderTypes}/{Commands,Queries}` — handlers own their `OrdersDbContext` logic
directly, same shape as `Organization`/`LeaveManagement`/`Location`/`Catalog`. Neither
`ICatalogPricingService` nor `ILocationDirectoryService` has an HTTP surface of its own here — both are
DI-only seams this module calls into (see Dependencies).

`OrdersPermissions.{Orders,Payments,OrderTypes}` each expose only `View`/`Manage` — not the four-way
`View`/`Create`/`Update`/`Delete` split most other modules use, same per-module simplification
`Location`/`Catalog` already use.

## Data Access

`OrdersDbContext : BaseDbContext`, schema `"orders"`, registered via
`Persistence.DbContextExtensions.AddConfiguredDbContext<OrdersDbContext>(configuration, DbConnectionNames.Orders)`.
`DbConnectionNames.Orders` aliases `DbConnectionNames.Default` ("DefaultConnection") — same physical
database/connection string as every other module, separated only by schema (`orders`) + table name.

Five tables. `Orders`/`OrderLines`/`OrderFees`/`Payments` are keyed by a database-generated
`bigint IDENTITY(1,1)` (`AuditableEntity<long>`, the same numeric-key base `Catalog.Api`'s `Product`
uses); `OrderTypes` has a composite string key (below):

- **`Orders`** — index on `LocationId`; unique index on `OrderCode` (a `HasConversion`-mapped scalar
  column, `HasMaxLength(OrderCode.MaxLength)` = 17, not an owned type — same treatment as `Catalog`'s
  `Sku`). `ConcurrencyToken` is a required `MaxLength(32)` `IsConcurrencyToken()` column, app-managed:
  `OrdersDbContext.RotateConcurrencyTokens()` rotates it (`Guid("N")`) on every modified `Order` during
  `SaveChanges[Async]` — same mechanism as `ApprovalRequest.ConcurrencyToken`. `LocationId`/`MemberId`
  max length 450, `CancelledReason` max length 1000, `ExternalReferenceCode` max length 50. `AmountPaid`
  is a required table-split owned `Money` (same row) — `AmountPaidAmount decimal(18,2)`/
  `AmountPaidCurrency` (max length 3); `ReconcilePaymentStatus` mutates it in place via `Money.Update`
  rather than reassigning. `Discount` is an **optional** table-split owned `OrderDiscount`
  (`DiscountKind`/`DiscountValue decimal(18,2)`, no `Navigation(...).IsRequired()`) — `null` when no
  discount is applied; an already-present one is mutated in place via `OrderDiscount.Update`, a
  cleared/first-assigned one is a plain reference assignment (see Notable Conventions).
  `Lines`/`Fees` are real `HasMany`/`WithOne` relationships into their own tables with
  `DeleteBehavior.Cascade` and a field-backed navigation (`PropertyAccessMode.Field`) — not owned
  collections, since each child has its own identity and is individually removable, same shape as
  `ApprovalRequest.Steps`/`ApprovalStep`.
- **`OrderLines`** — index on `OrderId`. `ProductName` max length 200, `Sku` max length 100, `OrderCode`
  max length 17 — all three are plain denormalized snapshot columns (no FK, no index) taken once at
  `Order.AddLine` time and never re-read from their source, so a line keeps showing what the customer
  was actually charged even if the product is later repriced/renamed. `UnitPrice`/`VatRate` are
  required table-split owned types (same row); `RequestedSalePrice` is an **optional** table-split
  owned `Money` — `null` when the line sells at `UnitPrice`, mutated in place via `Money.Update` when
  already present, plain assignment on first set or clear.
- **`OrderFees`** — indexes on `OrderId` and `FeeTypeId`. `Name` max length 200, `OrderCode` max length
  17 (denormalized snapshot, same treatment as `OrderLines.OrderCode`). `FeeTypeId` (max length 450) and
  `FeeTypeName` (max length 200) are plain columns — **no FK** to `OrderTypes` — holding the fee type's
  code and an immutable name snapshot. `Amount` is a required table-split owned `Money`.
- **`Payments`** — indexes on `OrderId` and `PaymentTypeId` — **no FK/navigation configured at all** to
  `Orders` (not even `Restrict`), because `Payment` is a separate aggregate root that deliberately never
  loads or locks the `Order` graph to record a payment (see Notable Conventions). `Reference` max length
  200, `RecordedByUserId` max length 450, `VoidReason` max length 1000, `OrderCode` max length 17
  (denormalized snapshot). `PaymentTypeId` (max length 450) / `PaymentTypeName` (max length 200) mirror
  the `OrderFees` type columns, again with no FK. `Amount` is a required table-split owned `Money`.
- **`OrderTypes`** — the fee/payment type catalog. Composite primary key `(Id, Category)` with
  `Id` `ValueGeneratedNever()` (a caller-supplied string code such as `SHIPPING`/`CASH`) — unique only
  within a `Category` (`Fee`/`Payment`), so the same code (e.g. `OTHER`) legitimately exists once per
  category. `Name` max length 200, plus a `Status` (`Active`/`Inactive`). Configured with the
  parameterless `ConfigureAuditableEntity()` (string-keyed audit columns).

All five call `entity.ConfigureAuditableEntity...()`; `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: false)` — no
entity implements `ISoftDelete`, so this module has no soft-delete support at all (contrast `Catalog`'s
`Product`). The synchronous `SaveChanges` override stays audit + token-rotation only, matching
`ApprovalDbContext`'s reasoning, because the write path is 100% async. `SaveChangesAsync` additionally
calls `IPublisher.DispatchDomainEvents(this)` after the commit, wrapped in a `try`/`catch` that logs a
warning so a faulting handler can never fail an already-committed write — same shape as
`ApprovalDbContext`. Unlike `Approval`, **nothing currently subscribes** to any of the three domain
events this dispatches (`OrderPlacedEvent`/`OrderCancelledEvent`/`OrderFulfilledEvent`) — the dispatch
call is a no-op today, built ahead of a consumer (see Notable Conventions).

Query handlers read `AsNoTracking`. `GetOrderByIdQueryHandler`/`SearchOrdersQueryHandler`/
`GetPaymentsByOrderQueryHandler` hand-map the materialised entity to its DTO directly (`ToDto`,
`internal static` and reused between the by-id and search handlers, same reuse pattern
`GetProductByIdQueryHandler.ToDto` follows in `Catalog`) rather than a Mapster projection — the owned
table-split `Money`/`VatPercentage`/`OrderDiscount` columns and the `HasConversion`-mapped `OrderCode`
load automatically with the entity. `Mapster` is still declared as a package reference in
`Orders.Api.csproj`, but nothing in this module currently calls into it.

`OrdersContextInitialiser.InitialiseAsync()` applies migrations; `TrySeedAsync()` idempotently seeds the
`OrderTypes` catalog (each row looked up by its composite `(Id, Category)` key before insert): fee types
`SHIPPING`/`OTHER`, payment types `CASH`/`CARD`/`BANK_TRANSFER`/`OTHER`. `src/Migrations/MSSQL/Program.cs`
calls both.

Migrations exist for **MSSQL only so far**: `src/Migrations/MSSQL/Orders/` holds incremental migrations
(starting at `CreateOrdersSchema`) — not yet a squashed baseline (per the dev-migration-squash
convention, squashing happens once a module is judged complete). The `PostgreSQL`/`Sqlite` migration
projects do not yet reference `Orders.Api` at all.

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Orders.Contracts → Shared`) | `BaseDto<long>` for every DTO's `Id`; `Money`/`VatPercentage` value objects and `CurrencyConstants.Default` consumed by the `Order`/`OrderLine`/`OrderFee`/`Payment` aggregates. |
| `Infrastructure` | project (`Orders.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Orders.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity<TEntity, TId>`, `DispatchDomainEvents`, paging extensions, `DbUpdateExceptionExtensions.IsUniqueConstraintViolation` (the `OrderCode` collision catch in `CreateOrderCommandHandler`). |
| `Catalog.Contracts` | project (`Orders.Api → Catalog.Contracts`) | `ICatalogPricingService`, consumed by `AddOrderLineCommandHandler` to resolve a product's current name/price/VAT rate/status. Orders is this seam's first real consumer (see [Catalog.md](Catalog.md)). |
| `Location.Contracts` | project (`Orders.Api → Location.Contracts`) | `ILocationDirectoryService.ExistsAsync`, consumed by `CreateOrderCommandHandler` to validate `LocationId` before creating an order. Orders is this seam's first real consumer (see [Location.md](Location.md)). |
| `Orders.Contracts` | project (`Orders.Api → Orders.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result`, `Mapster` (`Orders.Api`) | package, **all declared directly** | Same positive contrast as `Organization`/`Approval`/`LeaveManagement`/`Location`/`Catalog` — no undeclared-transitive-dependency instance. `Mapster` is declared but currently unused (see Data Access). |

`Orders.Contracts.csproj` also carries `ProjectReference`s to `Catalog.Contracts` and
`Location.Contracts`, but no type in `Orders.Contracts` actually uses either — every
cross-module-looking field in its own DTOs/requests (`OrderDto.LocationId`, `CreateOrderRequest.LocationId`,
`SearchOrderRequest.LocationId`) is a plain `string`, not a shared type from those projects. The real
cross-module dependency is one layer down, in `Orders.Api` (the table above).

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/MSSQL` — references `Orders.Api` directly for `OrdersDbContext`/
  `OrdersContextInitialiser`. `PostgreSQL`/`Sqlite` do not (see Data Access).
- `Orders.Tests` — `Orders.Api.csproj` grants `InternalsVisibleTo` to reach the `internal` command/query
  records and handlers.

No business module references `Orders.Api`/`Orders.Contracts` — confirmed via `ProjectReference` search
across `src/`. Orders is, so far, purely a **consumer** of other modules' seams (`Catalog`'s
`ICatalogPricingService`, `Location`'s `ILocationDirectoryService`), not a provider of one of its own —
unlike `Catalog`/`Location`, `OrdersModule.cs` registers no cross-module DI interface (`IOrderTypeCache`
is module-local, `internal` in implementation).

## Notable Conventions

- **`Order`/`OrderLine`/`OrderFee`/`Payment` all use the database-generated `bigint IDENTITY(1,1)`
  numeric-key base (`AuditableEntity<long>`)**, the same divergence from the repo's usual app-generated
  string-GUID id that `Catalog`'s `Product` introduced (see [Catalog.md](Catalog.md) Notable
  Conventions) — smaller/faster PKs, natural sort order, and compact values for the denormalized
  cross-aggregate snapshots this module leans on (`OrderLine.ProductId`, every child's `OrderCode`
  string). `OrderType` is the one exception: a string-keyed catalog entity (see below).
- **`OrderType` is a data-driven, admin-manageable catalog with a `Category` discriminator, replacing
  hardcoded fee-type/payment-method enums.** One flat entity serves both catalogs (`Fee`/`Payment`)
  rather than two near-identical tables; its `Id` is a caller-supplied string code, unique only per
  category, hence the composite `(Id, Category)` key — consequently every lookup, route, and command
  carries both parts. Category and id are immutable after creation; only name/status change. It mirrors
  `Location`'s `LocationType` catalog precedent (see [Location.md](Location.md)) but is flat — no
  hierarchy or parent rules.
- **`OrderTypeCache` is a hand-written, module-local, full-table cache** (`IOrderTypeCache`:
  `GetAllAsync`/`GetAsync(id, category)`/`ReloadAsync`), reloaded after every catalog write — deliberately
  not the generic `ICacheRepository<T>`, for the same reason `Location`'s `LocationTypeCache` avoids it
  (the write path is owned by this module's own handlers, so exclusivity holds by construction). It is
  registered scoped and used by `AddOrderFee`/`RecordPayment` to validate the type.
- **`OrderFee`/`Payment` reference their catalog entry by a plain string id, not an FK, plus an immutable
  name snapshot** (`FeeTypeId`/`FeeTypeName`, `PaymentTypeId`/`PaymentTypeName`) captured at creation —
  the same one-time-snapshot treatment as `OrderCode` and `OrderLine`'s product fields. `AddOrderFee`/
  `RecordPayment` look the type up in the fixed expected category (a fee request can never resolve a
  `Payment`-category entry of the same code) and require it to be `Active`; renaming, deactivating, or
  deleting a type afterwards never changes what an existing order displays, which is also why catalog
  deletion needs no in-use guard. DTOs expose both id and name.
- **`OrderCode` is a unique, human-readable reference, modeled directly on `Catalog`'s `Sku`** (plain
  sealed class, manual `Equals`/`GetHashCode`/`ToString`, self-validating ctor, not
  `Light.Domain.ValueObjects.ValueObject`, mapped as a `HasConversion` scalar with a real unique index —
  not an owned type). Two distinct shapes share the type: `OrderCode.Generate(now)` always produces the
  specific `yyyyMMdd` + 9-char Crockford Base32 system format (alphabet excludes `I`/`L`/`O`/`U` to
  avoid misreads); the public `OrderCode(string)` constructor is intentionally looser (non-blank, within
  the 17-char column width) because `CreateOrderRequest.OrderCode` also accepts a caller-supplied value
  that isn't guaranteed to follow the generated shape.
- **Two distinct order-creation paths, split by whether `CreateOrderRequest.OrderCode` was supplied.**
  A caller-chosen code is pre-checked for uniqueness and, on an unlikely concurrent-insert race, caught
  via `DbUpdateException`/`IsUniqueConstraintViolation()` and returned as a friendly conflict — it is
  **never** silently swapped out for a different value. An omitted code is generated
  (`OrderCode.Generate`) and wrapped in a bounded 3-attempt retry loop
  (`CreateOrderCommandHandler.CreateWithGeneratedCodeAsync`): each collision calls
  `Order.RegenerateOrderCode` and retries, since a pre-check cannot meaningfully protect against a
  random draw's own future collision; exhausting all attempts throws (treated as exceptional, not a
  `Result` to swallow).
- **`OrderLine`/`OrderFee`/`Payment` each carry a denormalized `OrderCode` string snapshot of their
  parent `Order`**, taken once at creation alongside their `OrderId` FK/reference — so any row from any
  of the four tables is self-describing without a join back to `Orders`. This mirrors the pre-existing
  pattern where `OrderLine.ProductId`/`ProductName`/`Sku`/`UnitPrice`/`VatRate` are themselves one-time
  snapshots taken from `Catalog` at `AddLine` time and never re-read — a line must keep showing what the
  customer was actually charged even if the product is later repriced or renamed.
- **`OrderLine`/`OrderFee` are real child entities in a normal `HasMany`/`WithOne` relationship, not
  owned types** — own `Id`, individually removable while the order is a draft — the same shape
  `ApprovalRequest.Steps`/`ApprovalStep` uses, and a deliberate contrast with `Catalog`'s
  `ProductImageUrl` (a true owned collection, since an image has no independent identity or removal
  granularity beyond "in the list or not").
- **`Payment` is a separate aggregate root, not a child of `Order`.** It shares `OrdersDbContext`/the
  same database but holds only a loose `OrderId` (and an `OrderCode` snapshot) with **no configured
  FK/navigation at all** — the boundary is deliberate so recording or voiding a payment never loads or
  locks the entire order graph. `RecordPaymentCommandHandler`/`VoidPaymentCommandHandler` instead load
  the `Order` separately, sum the other non-voided payments by hand (excluding the one just
  added/voided, since its own `IsVoided`/persistence state isn't committed yet at that point), and pass
  the total into `Order.ReconcilePaymentStatus` — the aggregate itself never queries `Payments`
  directly. Voiding a payment does not delete it; the audit trail (who recorded it, who voided it, why)
  is kept.
- **`Order.Subtotal`/`DiscountAmount`/`FeesTotal`/`Total` are computed as plain `decimal`, not `Money`.**
  A still-editable draft can transiently carry a discount larger than its subtotal (`ApplyDiscount`/
  `AddFee` do not cap against the subtotal as they happen) — that invariant is enforced only at `Place`
  time. Using `Money` for these roll-ups would let its own constructor guard throw on a negative
  intermediate value before the dedicated "discount cannot exceed subtotal"/"total cannot be negative"
  checks in `Place` ever got a chance to run.
- **`OrderDiscount` is an optional owned value object** (`Light.Domain.ValueObjects.ValueObject`, unlike
  `Sku`/`OrderCode`) — `null` when no discount is applied. `Order.ApplyDiscount` mutates an
  already-present instance in place via `OrderDiscount.Update` rather than reassigning it (same
  tracked-owned-reference hazard `Money`/`VatPercentage` guard against, see [Catalog.md](Catalog.md));
  the very first assignment and `Order.RemoveDiscount`'s clear back to `null` are plain reference
  assignments instead, since an `Added`/`Deleted` owned-entity transition doesn't hit that hazard.
- **`Money.Update`/`VatPercentage.Update` extend to this module's owned `Money` fields** (`Order.AmountPaid`,
  `OrderLine.RequestedSalePrice`, and every table-split `Money` amount) for the same
  tracked-owned-reference reason documented on `Catalog`'s `Product.Reprice` — `Shared.csproj` grants a
  scoped `InternalsVisibleTo("StarterKit.Orders.Api")` to reach `Update` (see [Catalog.md § Notable
  Conventions](Catalog.md#notable-conventions)).
- **App-managed optimistic concurrency exists on `Order` (`ConcurrencyToken`, rotated every `SaveChanges[Async]`),
  but — unlike `ApprovalRequest` — no command handler in this module currently catches
  `DbUpdateConcurrencyException`.** `PlaceOrder`/`CancelOrder`/`RecordPayment`/`VoidPayment` and every
  line/fee/discount editor call `SaveChangesAsync` directly; a genuine concurrent write against the same
  `Order` will surface as an unhandled `DbUpdateConcurrencyException` rather than the detach-reload-retry
  pattern `ApprovalService.DecideAsync`/`CancelAsync` use.
- **Three domain events are queued but currently have no handler.** `Order.Place`/`Cancel`/`MarkFulfilled`
  each call `AddDomainEvent` (`OrderPlacedEvent`/`OrderCancelledEvent`/`OrderFulfilledEvent`, all
  `internal sealed record : DomainEvent`), and `OrdersDbContext.SaveChangesAsync` dispatches them via
  `IPublisher.DispatchDomainEvents(this)` post-commit (same best-effort, logged-not-thrown shape as
  `ApprovalDbContext`) — but no `INotificationHandler<T>` in this module (or any other, confirmed via
  search) subscribes to any of the three yet. Built ahead of a consumer, the same "seam with no current
  consumer" shape `ICatalogPricingService`/`ILocationDirectoryService` had before Orders itself became
  their first consumer.
- **`OrderPlacedIntegrationEvent` (the `Contracts`-level, cross-module notification) is published
  separately from the in-process domain events above**, directly by `PlaceOrderCommandHandler` after
  `SaveChangesAsync` commits — best-effort, wrapped in its own `try`/`catch` that logs a warning rather
  than failing the placement call. Same best-effort-immediate-delivery contract as `Approval`'s
  `ApprovalFinalizedIntegrationEvent`, but published from the command handler rather than from inside
  `OrdersDbContext`'s domain-event dispatch.
- **`OrdersPermissions` has only `View`/`Manage` per feature, not the four-way
  `View`/`Create`/`Update`/`Delete` split most other modules use** — same per-module simplification
  `Location`/`Catalog` already use.
- **Migration is MSSQL-only and unsquashed** — see Data Access. Treat `Orders` as mid-development, not
  yet at the "template baseline" state `Organization`/`Approval`/`LeaveManagement` are in.
- `Specification<T>` (vendor `Light.Specification`) is used only for the by-id lookups (`OrderByIdSpec`,
  `PaymentByIdSpec`, `OrderTypeByIdSpec`), reused across several handlers each — same policy as every
  other module.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-19_
