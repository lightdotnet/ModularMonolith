# Module Overview: Catalog

## Purpose

Owns the product catalog via two aggregates. `Category` is a self-referencing entity forming a
category tree — a simpler direct sibling of `Location`'s `Location`/`LocationType` pair: no type
discriminator, no allowed-parent-type rule, so `Create`/`Rename`/`Move` carry no real domain invariant
of their own beyond keeping every state transition behind a guarded method. `Product` is the sellable
item — SKU, price, VAT rate, category assignment, and a small gallery of image URLs — with every
transition (`Create`/`Rename`/`UpdateDescription`/`Reprice`/`UpdateVatRate`/`Recategorize`/
`AddImage`/`RemoveImage`/`Activate`/`Deactivate`/`UpdateSku`/`Delete`) behind a guarded method. Unlike
every other aggregate in this repo (including `Category`), `Product.Id` is a database-generated
`bigint IDENTITY(1,1)` rather than an app-generated string GUID, and `Product` is the repo's first
active soft-delete instance (`ISoftDelete`, backed by a query filter that hides deleted rows from
every default read) — see Notable Conventions for both. `Sku` is nullable: `UpdateSku(null)` frees it
for reuse by a new product, but a soft-deleted product's SKU is not freed automatically — see Notable
Conventions for the reuse mechanics. Pricing uses two genuinely shared-kernel value objects,
`Money`/`VatPercentage` (`src/Shared/ValueObjects/`) — `Product` was their first consumer;
`Orders.Api`'s `Order`/`OrderLine`/`OrderFee`/`Payment` now also consume `Money`, and `OrderLine`
consumes `VatPercentage` too (see [Orders.md](Orders.md), and
[../architecture.md § Shared Kernel](../architecture.md#shared-kernel--common-building-blocks)). A
product's price currency may be any **active** currency known to the `Currency` module. The
module also exposes `ICatalogPricingService`, a read-only cross-module seam (same role as Location's
`ILocationDirectoryService`) that `Orders.Api`, `Transfers.Api`, and `Purchasing.Api` consume to resolve a
product's current name/price/VAT rate/status without reaching into this module's aggregate or EF internals.

## Internal Layering

Catalog is a **single-project module** (not split Domain/Application/Infrastructure/Api), following
the same structural convention as `Organization`/`Approval`/`LeaveManagement`/`Location`:

| Project | Responsibility | Notes |
|---|---|---|
| `Catalog.Contracts` | DTOs, requests, enums, and the permission catalog, organized into per-feature subfolders — `Common/` (`ProductStatus`: `Active`/`Inactive`), `Categories/` (`CategoryDto`, `CategoryTreeNodeDto` (adds a `Children` list), `CreateCategoryRequest`, `UpdateCategoryRequest`, `MoveCategoryRequest`), `Products/` (`ProductDto` (flattens `Money`/`VatPercentage` to scalar `Price`/`Currency`/`VatRate`, never a nested value-object shape; nullable `Sku`), `ProductImageDto`, `ProductPriceInfoDto` (the cross-module pricing projection, also nullable `Sku`), `ProductSearchRequest`, `UpsertProductRequest`, `AddProductImageRequest`), `Services/` (`ICatalogPricingService` — the module's cross-module seam, keyed by `long productId`, see Notable Conventions), `Authorization/` (`CatalogPermissions`, `CatalogPermissionProvider`). Every Request DTO carries its own `AbstractValidator<TRequest>` **in the same file** — the two-layer FluentValidation convention `Location` established (see Notable Conventions). Declares `Lightsoft.AspNetCore.Authorization` directly. |
| `Catalog.Api` | Single project organized by folder: `Domain/{Categories,Products}` (`Domain/Categories/Category.cs` + `CategoryByIdSpec.cs`; `Domain/Products/Product.cs` (`AuditableEntity<long>`, `ISoftDelete`) + `Sku.cs` + `ProductImageUrl.cs` + `ProductByIdSpec.cs` — see Notable Conventions for the `Specification<T>` pattern), `Data/` (`CatalogDbContext`, `CatalogContextInitialiser`), `Application/{Categories,Products}/{Commands,Queries}` (every handler owns its business logic directly against `CatalogDbContext` — no service-class indirection — plus a thin per-command `AbstractValidator`, see Notable Conventions; `Products/Commands` includes `UpsertProduct`/`ActivateProduct`/`DeactivateProduct`/`AddProductImage`/`RemoveProductImage`/`DeleteProduct`), `Services/` (`CatalogPricingService`, `internal`, implementing `ICatalogPricingService`), `Controllers/` (`CategoryController`, `ProductController`), `CatalogModule.cs` (DI: DbContext + `ICatalogPricingService` + permission provider). |

## Public Contract

`CategoryController` (route `category`, `[MustHavePermission(CatalogPermissions.Categories.View)]` at
class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/category/tree` | GET | `catalog.categories.view` | — | `IList<CategoryTreeNodeDto>` — flat table loaded once, assembled into a tree in memory by grouping on `ParentCategoryId` (same pattern as Location/Organization's tree endpoints) |
| `api/v{version}/category/{id}` | GET | `catalog.categories.view` | Route `id` | `Result<CategoryDto>` (Mapster `ProjectToType`) |
| `api/v{version}/category/{id}/children` | GET | `catalog.categories.view` | Route `id` | `IList<CategoryDto>` — immediate children only |
| `api/v{version}/category` | POST | `catalog.categories.manage` | `CreateCategoryRequest` | `Result<string>` (new id); validates an optional parent exists, pre-checks duplicate sibling name under the same parent (known root-level gap — see Notable Conventions), then `Category.Create` |
| `api/v{version}/category/{id}` | PUT | `catalog.categories.manage` | `UpdateCategoryRequest` | `Result`; renames, pre-checks duplicate sibling name (excluding self) |
| `api/v{version}/category/{id}/move` | PUT | `catalog.categories.manage` | `MoveCategoryRequest { NewParentCategoryId }` | `Result`; guards: can't parent to itself, new parent must exist, can't move under one of its own descendants (walked iteratively via `ParentCategoryId`, one round-trip per ancestor level — same pattern as Location/Organization's move handlers), then `Category.Move` |
| `api/v{version}/category/{id}` | DELETE | `catalog.categories.manage` | Route `id` | `Result`; blocked if the category still has child categories or products assigned |

`ProductController` (route `product`, `[MustHavePermission(CatalogPermissions.Products.View)]` at
class level; every route id is `long`). `Category` keeps separate Create/Update commands, but
`Product` upserts through one command — see Notable Conventions for why:

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/product` | GET | `catalog.products.view` | `ProductSearchRequest` (`SearchQuery` base — `SearchValue` + paging — plus `CategoryId`/`Status` filters) | `PagedResult<ProductDto>`, filtered by category/status/`Name.Contains(SearchValue)`, ordered by `Created` desc |
| `api/v{version}/product/{id}` | GET | `catalog.products.view` | Route `id` | `Result<ProductDto>` — hand-mapped from the materialised entity, not an EF `Select` projection (see Notable Conventions) |
| `api/v{version}/product/{id?}` | PUT | `catalog.products.manage` | `UpsertProductRequest` | `Result<long>` (the product's id, new or existing); validates the category exists, checks the price currency is an active currency (see Notable Conventions), and pre-checks SKU uniqueness with `IgnoreQueryFilters()` (equality filter on the `HasConversion`-mapped `Sku`, see Notable Conventions); `id` omitted constructs `Sku`/`Money`/`VatPercentage` and calls `Product.Create` (`Sku` required on this path), `id` present loads the entity (`.Include(x => x.Images)`) and calls `Rename`+`UpdateDescription`+`Reprice`+`UpdateVatRate`+`Recategorize`+`UpdateSku` together (`Sku` optional — empty clears it); both paths replace `Images` wholesale (`RemoveImages()` then re-`AddImage` every entry in the request, not a diff) |
| `api/v{version}/product/{id}/activate` | PUT | `catalog.products.manage` | Route `id` | `Result`; `Product.Activate` |
| `api/v{version}/product/{id}/deactivate` | PUT | `catalog.products.manage` | Route `id` | `Result`; `Product.Deactivate` |
| `api/v{version}/product/{id}/image` | POST | `catalog.products.manage` | `AddProductImageRequest { Url, SortOrder? }` | `Result`; `Product.AddImage` |
| `api/v{version}/product/{id}/image` | DELETE | `catalog.products.manage` | Query `url` | `Result`; `Product.RemoveImage(url)` removes every image matching the url |
| `api/v{version}/product/{id}` | DELETE | `catalog.products.manage` | Route `id` | `Result`; `Product.Delete()` guards `Status == Inactive` (throws `ConflictException` otherwise), then `context.Products.Remove(entity)` — the soft-delete plumbing intercepts that `Remove()` call (see Notable Conventions) |

Every action across both controllers dispatches a mediator command/query under
`Application/{Categories,Products}/{Commands,Queries}` — handlers own their `CatalogDbContext` logic
directly, same shape as `Organization`/`LeaveManagement`/`Location`. `ICatalogPricingService` (see
Notable Conventions) is a DI-only seam with no HTTP surface of its own; see Depended On By for its
consumers.

`CatalogPermissions.{Categories,Products}` each expose only `View`/`Manage` — not the
`View`/`Create`/`Update`/`Delete` four-way split most other modules use, mirroring Location's
per-module simplification.

## Data Access

`CatalogDbContext : BaseDbContext`, schema `"catalog"`, registered via
`Persistence.DbContextExtensions.AddConfiguredDbContext<CatalogDbContext>(configuration, DbConnectionNames.Catalog)`.
`DbConnectionNames.Catalog` aliases `DbConnectionNames.Default` ("DefaultConnection") — same physical
database/connection string as every other module, separated only by schema (`catalog`) + table name.

Three tables:

- **`Categories`** — unique index on `(ParentCategoryId, Name)`; index on `ParentCategoryId`.
  Self-referencing `Parent`/`Children` FK (`ParentCategoryId`) is `DeleteBehavior.Restrict`. `Name` max
  length 200, `ParentCategoryId` max length 450. `Id` stays the repo's usual app-generated string GUID
  (`AuditableEntity`), unaffected by `Product.Id`'s retype (see below).
- **`Products`** — filtered unique index on `Sku` (where `Sku` is not null, written with
  `HasProviderFilter` so the filter text is right per provider; deliberately not scoped
  by `Deleted` — see Notable Conventions); index on `CategoryId`. FK to `Category` (`CategoryId`) is
  `Restrict`. `Name` max length 200, `Description` max length 2000, `CategoryId` max length 450. `Id`
  is a database-generated `bigint IDENTITY(1,1)` (`AuditableEntity<long>`) — `ConfigureAuditableEntity<Product, long>()`
  skips the `Id`-length configuration the string-keyed overload applies to `Category`, letting the
  provider's own numeric-key convention take over (see Notable Conventions). `Sku` is a nullable
  converted scalar column (`HasConversion`, null-safe both ways, max length `Sku.MaxLength` = 100)
  rather than an owned type. `Price`/`VatRate` are table-split EF owned types (same row) —
  `PriceAmount decimal(18,2)`/`PriceCurrency` (max length 3), `VatRate decimal(5,2)`;
  `Reprice`/`UpdateVatRate` mutate the tracked owned instance in place via `Money.Update`/
  `VatPercentage.Update` rather than reassigning (see Notable Conventions). `Product` implements
  `ISoftDelete` (`Deleted`/`DeletedBy`) and carries `entity.HasQueryFilter(x => x.Deleted == null)` —
  the first active query filter in this repo (see Notable Conventions).
- **`ProductImages`** — owned collection (`OwnsMany`) into its own table with a shadow `int Id`
  surrogate key as the sole PK (not composite with `ProductId`) — Sqlite only auto-populates an
  `INTEGER PRIMARY KEY` via its rowid-alias optimization when that column is the sole PK member;
  `ProductId` is a plain indexed FK column instead, typed `bigint` following `Product.Id`. `Url` max
  length 2048.

`Category` calls `entity.ConfigureAuditableEntity()` (the string-keyed overload); `Product` calls the
numeric-key sibling `entity.ConfigureAuditableEntity<Product, long>()`. `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: true)` —
flipped to `true` because `Product` is the first entity anywhere in this repo to implement
`ISoftDelete`; `AuditEntries` only stamps `Deleted`/`DeletedBy` on entries whose runtime type actually
implements the interface, so `Category` (not `ISoftDelete`) is unaffected by the flip.

Query handlers read `AsNoTracking`. `GetCategoryByIdQueryHandler`/`GetCategoryChildrenQueryHandler` use
Mapster's `ProjectToType<T>`; `GetCategoryTreeQueryHandler` loads the full flat table once and
assembles the tree in memory via `GroupBy(x => x.ParentCategoryId ?? string.Empty)` — no recursive
CTE, same approach as Location/Organization's tree endpoints. `GetProductByIdQueryHandler`/
`ListProductsQueryHandler`/`CatalogPricingService` hand-map the materialised `Product` entity to its
DTO instead of an EF `Select` projection — `Sku` is a `HasConversion`-mapped scalar and `Price`/
`VatRate` are owned-type table-split columns that load automatically with the entity, and there is no
established precedent in this repo for composing further member access (`x.Sku.Value`) inside a
server-translated `Select`; loading the entity sidesteps the question (see `CatalogPricingService`'s
class doc). The default query (used by all three) transparently excludes soft-deleted products via the
`Product` query filter; `UpsertProductCommandHandler`'s SKU-uniqueness pre-check (both the create and
update path) calls `IgnoreQueryFilters()` deliberately (see Notable Conventions).

`CatalogContextInitialiser.InitialiseAsync()` applies migrations; `TrySeedAsync()` idempotently seeds a
small set of sample categories and products at runtime (looked up by name/parent and by SKU before
insert), each product priced in `CurrencyConstants.Default`. The seed rows come from the initialiser, not from
migrations. Each provider's migrator `Program.cs` calls both.

Migrations: see [../../conventions/migrations.md](../../conventions/migrations.md).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Catalog.Contracts → Shared`) | `BaseDto`/`BaseDto<long>` for `CategoryDto`/`CategoryTreeNodeDto`/`ProductDto`/`ProductPriceInfoDto`; the `Money`/`VatPercentage` value objects consumed by the `Product` aggregate; `CurrencyConstants.Default` (the seeder's price currency). |
| `Infrastructure` | project (`Catalog.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Catalog.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity`/`ConfigureAuditableEntity<TEntity, TId>`, `HasProviderFilter`. |
| `Currency.Contracts` | project (`Catalog.Api → Currency.Contracts`) | `ICurrencyService.GetAsync`, consumed by `UpsertProductCommandHandler` to check that the price currency exists and is active (see [Currency.md](Currency.md)). |
| `Catalog.Contracts` | project (`Catalog.Api → Catalog.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result`, `Mapster` (`Catalog.Api`) | package, **all declared directly** | Same positive contrast as `Organization`/`Approval`/`LeaveManagement`/`Location` — no undeclared-transitive-dependency instance. |

`Catalog`'s only outgoing dependency on another business module is `Currency.Contracts`.

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` — each references `Catalog.Api` directly for `CatalogDbContext`/
  `CatalogContextInitialiser`.
- `Catalog.Tests` — `Catalog.Api.csproj` grants `InternalsVisibleTo` to reach the `internal`
  command/query records and handlers.
- `Orders.Api`/`Orders.Contracts` — both project-reference `Catalog.Contracts`. `Orders.Api`'s
  `AddOrderLineCommandHandler` calls `GetPriceInfoAsync(productId)` to resolve a product's current
  name/price/currency/VAT rate/status and snapshots them onto the new `OrderLine`, rejecting the add if the
  product is missing or not `Active` (see [Orders.md](Orders.md)). `Orders.Contracts.csproj` also carries a
  `ProjectReference` to `Catalog.Contracts`, but no type in `Orders.Contracts` itself currently uses it —
  every cross-module-looking field in its own DTOs (e.g. `LocationId`) is a plain `string`, not a shared
  type; only `Orders.Api` actually consumes the seam.
- **`Transfers.Api`** — `AddStockTransferLineCommandHandler` calls `GetPriceInfoAsync` for the product
  name/SKU snapshot (see [Transfers.md](Transfers.md)).
- **`Purchasing.Api`** — `AddPurchaseOrderLineCommandHandler` calls `GetPriceInfoAsync` for the product
  name/SKU snapshot; the catalog sell price is irrelevant to a purchase cost (see [Purchasing.md](Purchasing.md)).

## Notable Conventions

- **`Category` is a simpler direct sibling of `Location`** — no type discriminator, no
  allowed-parent-type rule, so `Create`/`Rename`/`Move` carry no real domain invariant of their own;
  they exist to keep every state transition behind a guarded method rather than an open setter. Cycle
  detection needs a database walk and is enforced by the caller (`MoveCategoryCommandHandler`), not
  here — same split as `MoveLocationCommandHandler`/`MoveOrgUnitCommandHandler`.
- **Known gap: root-level category name collisions.** Sibling-name uniqueness is enforced by a unique
  `(ParentCategoryId, Name)` DB index plus a handler pre-check, but the index treats two `NULL`
  `ParentCategoryId` rows as distinct — so under a race, two root-level categories can still end up
  with the same name. Documented on `Category`'s class doc; not yet tracked as a `known-debt.md` item.
- **`Product.Id` is a database-generated `bigint IDENTITY(1,1)`, not the repo's usual app-generated
  string GUID.** `Category.Id` keeps the usual convention — `Product` (along with every id in
  `Orders.Api`'s `Order`/`OrderLine`/`OrderFee`/`Payment` family) is instead built on the numeric-key
  `AuditableEntity<long>` base rather than the string-keyed `AuditableEntity`: smaller/faster PKs,
  natural sort order, and a compact value for `OrderLine.ProductId`'s cross-module snapshot to carry.
  `ConfigureAuditableEntity<Product, long>()` (`Persistence/Extensions/EntityTypeBuilderExtensions.cs`)
  is the numeric-key sibling of the string-keyed `ConfigureAuditableEntity()` `Category` still uses —
  it skips the `Id`-length configuration the string overload applies, since a numeric identity column
  needs none.
- **`Product` is the first active soft-delete instance in this repo.** Implements `ISoftDelete`
  (`Deleted`/`DeletedBy`, both set only through the interface reference by
  `TrackingExtensions.AuditEntries`, never directly by application code). `Product.Delete()` is a
  guarded domain method — callable only when `Status == ProductStatus.Inactive`, throws
  `ConflictException` otherwise — so a sellable product can't vanish out from under an active catalog;
  `DeleteProductCommandHandler` calls `Delete()` then `context.Products.Remove(entity)`, and that
  `Remove()` call is what `AuditEntries(..., enableSoftDelete: true)` intercepts and turns into a
  stamped `Deleted`/`DeletedBy` update instead of a real `DELETE`. `CatalogDbContext.ConfigureModel`
  adds `entity.HasQueryFilter(x => x.Deleted == null)` on `Product` — the first active query filter in
  this repo, the precedent for the next soft-deletable aggregate. `Category` does not implement
  `ISoftDelete` and is unaffected.
- **`Money`/`VatPercentage` are genuinely shared-kernel value objects** (`src/Shared/ValueObjects/`),
  not Catalog-specific — `Product` was their first consumer; `Orders.Api`'s `Order`/`OrderLine`/
  `OrderFee`/`Payment` now also consume `Money` (and `OrderLine` consumes `VatPercentage`). Both guard
  construction and expose an `internal Update` that mutates the tracked instance in place rather than
  being reassigned (same `DateRange.Update`/`ActiveStatus.Update` pattern `LeaveManagement`
  established) — reassigning an owned reference makes EF's change tracker emit the old instance as
  `Deleted`, which `TrackingExtensions.AuditEntries` resets back to `Unchanged` to guard against
  nulling the owned columns, but that reset then leaves the new values unpersisted. Since they live in
  a different assembly than either consumer, `Shared.csproj` grants a scoped
  `InternalsVisibleTo("StarterKit.Catalog.Api")` and `InternalsVisibleTo("StarterKit.Orders.Api")` to
  reach `Update`, alongside its existing `InternalsVisibleTo("Framework.Tests")`. See
  [../architecture.md § Shared Kernel](../architecture.md#shared-kernel--common-building-blocks).
- **A product's price currency must be an active currency, checked in the handler.**
  `UpsertProductCommandHandler` asks `ICurrencyService.GetAsync` and returns a not-found result when the code
  is unknown or inactive. On an update, an **unchanged** currency is not re-checked, so a product whose
  currency was deactivated later can still be edited; only a currency change has to land on an active one.
  `Money` itself only checks the code's ISO-4217 shape.
- **`Product` uses a single `UpsertProductCommand` instead of separate Create/Update commands** —
  `Id is null` creates, `Id is { }` updates, sharing one `UpsertProductRequest` field set for both
  paths (including `Images`, so images are settable at creation time too, not only after). `Category`
  in this same module still keeps separate `CreateCategoryCommand`/`UpdateCategoryCommand` — the split
  is a deliberate per-aggregate call, not a module-wide convention change: `UpsertProductCommand`
  exists so the admin client's product form can submit one shape for both create and edit.
- **`Sku` is a nullable, converted-scalar value object (`HasConversion`), not an owned type** —
  deliberately does not derive from `Light.Domain.ValueObjects.ValueObject` (that base exists to
  survive the tracked-owned-type `Deleted`/`Added` replace hazard described below, which only applies
  to owned navigations, not a converted scalar) — the same shape `Orders.Api`'s `OrderCode` is
  explicitly modeled on. `Product.UpdateSku(string? sku)` is the one mutator covering all three cases —
  set at `Create`, reassigned to a different value, or cleared to `null` — called from
  `UpsertProductCommandHandler`'s update path; the request-level `Sku` is optional, but
  `UpsertProductCommandValidator` requires it `NotEmpty` when `Id is null` (creating), since only the
  command — not `UpsertProductRequestValidator` — knows whether this is a create or an update. Reuse is
  deliberately **not** automatic on soft-delete: the filtered unique index (where `Sku` is not null,
  see Data Access) is *not* scoped by `Deleted`, so a soft-deleted product's SKU keeps blocking reuse
  until an explicit `UpdateSku(null)` (via the same upsert endpoint) clears it — only then can a new
  product claim that SKU value. `UpsertProductCommandHandler`'s SKU-uniqueness pre-check (both paths)
  calls `IgnoreQueryFilters()` for this reason — without it, a soft-deleted holder's SKU would look
  "available" right up until `SaveChanges` threw a raw DB unique-constraint violation. Uniqueness
  itself stays belt-and-suspenders: the handler pre-check plus this DB index.
- **`ProductImageUrl` is an owned collection (`OwnsMany`) with no in-place `Update`** — unlike `Money`/
  `VatPercentage`'s single table-split references, a collection member is always fully added or
  removed via `Product.AddImage`/`RemoveImage`, so it never needs one. It also derives from
  `Light.Domain.ValueObjects.ValueObject` (equality on `Url`+`SortOrder`) — unlike `Sku` above, this
  one *is* an owned navigation, just a collection one rather than a table-split reference. That
  distinction is why `TrackingExtensions.AuditEntries`'s `Deleted`→`Unchanged` reset guard (written for
  `Money`/`VatPercentage`-style table-split owned references, see the bullet above) is scoped to
  `x.Metadata.FindOwnership() is { IsUnique: true }` — `true` for a table-split `OwnsOne`, `false` for
  an `OwnsMany` collection — so a genuinely removed `ProductImageUrl` row keeps its `Deleted` state and
  is actually deleted, instead of being silently reset back to `Unchanged` (which used to make
  `RemoveImage`/an image-list replace a silent no-op). `UpsertProductCommandHandler`'s update path
  always `.Include(x => x.Images)` before touching them, then replaces the list wholesale
  (`RemoveImages()` + re-`AddImage` every entry) rather than diffing it.
- **Domain aggregates hold only real domain rules — not input-shape validation** — the same
  project-wide convention `Location` established (see
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md)).
  `CreateCategoryCommandHandler`'s parent-existence/name-uniqueness checks and
  `UpsertProductCommandHandler`'s category-existence/SKU-uniqueness/currency checks are handler-level
  guards, not domain invariants.
- **FluentValidation, two layers** — the same convention `Location` introduced (see
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md)): each `Contracts`
  request DTO carries an `AbstractValidator<TRequest>` in the same file (field-shape rules only); each
  mediator command has a thin `AbstractValidator<TCommand>` (same file as the command+handler)
  validating the route-level `Id` (`GreaterThan(0)` for `Product`'s numeric id, `NotEmpty` for
  `Category`'s string id) directly and delegating to the Contracts validator via
  `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. `UpsertProductCommandValidator` also
  carries the one rule that can't live on `UpsertProductRequestValidator` — `Sku` required only when
  `Id is null` — since the request validator has no visibility into whether this is a create or update.
- **`CatalogPermissions` has only `View`/`Manage`, not the four-way `View`/`Create`/`Update`/`Delete`
  split most other modules use** — mirrors `Location`'s per-module simplification.
- **`ICatalogPricingService`/`CatalogPricingService` is the module's cross-module DI seam**, same role
  as Location's `ILocationDirectoryService` — `GetPriceInfoAsync`/`GetPriceInfoBatchAsync`, both
  read-only and `AsNoTracking`, keyed by `long productId`/`IEnumerable<long> productIds`. Its consumers
  are listed under Depended On By; since the service reads through `CatalogDbContext`'s default query, a
  soft-deleted product is transparently excluded from pricing lookups the same way it's excluded from
  `GetProductById`/`ListProducts`.
- `Specification<T>` (vendor `Light.Specification`) is used only for the by-id lookups
  (`CategoryByIdSpec`, `ProductByIdSpec`), reused across several handlers each — same policy as
  Organization's/Location's.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
