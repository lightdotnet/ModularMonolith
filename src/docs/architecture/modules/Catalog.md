# Module Overview: Catalog

## Purpose

Owns the product catalog via two aggregates. `Category` is a self-referencing entity forming a
category tree — a simpler direct sibling of `Location`'s `Location`/`LocationType` pair: no type
discriminator, no allowed-parent-type rule, so `Create`/`Rename`/`Move` carry no real domain invariant
of their own beyond keeping every state transition behind a guarded method. `Product` is the sellable
item — SKU, price, VAT rate, category assignment, and a small gallery of image URLs — with every
transition (`Create`/`Rename`/`UpdateDescription`/`Reprice`/`UpdateVatRate`/`Recategorize`/
`AddImage`/`RemoveImage`/`Activate`/`Deactivate`) behind a guarded method. Pricing uses two genuinely
shared-kernel value objects, `Money`/`VatPercentage` (`src/Shared/ValueObjects/`) — `Product` is their
only consumer today (see [../architecture.md § Shared Kernel](../architecture.md#shared-kernel--common-building-blocks)).
The module also exposes `ICatalogPricingService`, a read-only cross-module seam (same role as
Location's `ILocationDirectoryService`) intended for a future Orders/Inventory-style module to resolve
product pricing without reaching into this module's aggregate or EF internals — it has no consumer yet.

## Internal Layering

Catalog is a **single-project module** (not split Domain/Application/Infrastructure/Api), following
the same structural convention as `Organization`/`Approval`/`LeaveManagement`/`Location`:

| Project | Responsibility | Notes |
|---|---|---|
| `Catalog.Contracts` | DTOs, requests, enums, and the permission catalog, organized into per-feature subfolders — `Common/` (`ProductStatus`: `Active`/`Inactive`), `Categories/` (`CategoryDto`, `CategoryTreeNodeDto` (adds a `Children` list), `CreateCategoryRequest`, `UpdateCategoryRequest`, `MoveCategoryRequest`), `Products/` (`ProductDto` (flattens `Money`/`VatPercentage` to scalar `Price`/`Currency`/`VatRate`, never a nested value-object shape), `ProductImageDto`, `ProductPriceInfoDto` (the cross-module pricing projection), `ProductSearchRequest`, `CreateProductRequest`, `UpdateProductRequest`, `AddProductImageRequest`), `Services/` (`ICatalogPricingService` — the module's cross-module seam, see Notable Conventions), `Authorization/` (`CatalogPermissions`, `CatalogPermissionProvider`). Every Request DTO carries its own `AbstractValidator<TRequest>` **in the same file** — the two-layer FluentValidation convention `Location` established (see Notable Conventions). Declares `Lightsoft.AspNetCore.Authorization` directly. |
| `Catalog.Api` | Single project organized by folder: `Domain/{Categories,Products}` (`Domain/Categories/Category.cs` + `CategoryByIdSpec.cs`; `Domain/Products/Product.cs` + `Sku.cs` + `ProductImageUrl.cs` + `ProductByIdSpec.cs` — see Notable Conventions for the `Specification<T>` pattern), `Data/` (`CatalogDbContext`, `CatalogContextInitialiser`), `Application/{Categories,Products}/{Commands,Queries}` (every handler owns its business logic directly against `CatalogDbContext` — no service-class indirection — plus a thin per-command `AbstractValidator`, see Notable Conventions), `Services/` (`CatalogPricingService`, `internal`, implementing `ICatalogPricingService`), `Controllers/` (`CategoryController`, `ProductController`), `CatalogModule.cs` (DI: DbContext + `ICatalogPricingService` + permission provider). |

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
class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/product` | GET | `catalog.products.view` | `ProductSearchRequest` (`SearchQuery` base — `SearchValue` + paging — plus `CategoryId`/`Status` filters) | `PagedResult<ProductDto>`, filtered by category/status/`Name.Contains(SearchValue)`, ordered by `Created` desc |
| `api/v{version}/product/{id}` | GET | `catalog.products.view` | Route `id` | `Result<ProductDto>` — hand-mapped from the materialised entity, not an EF `Select` projection (see Notable Conventions) |
| `api/v{version}/product` | POST | `catalog.products.manage` | `CreateProductRequest` | `Result<string>` (new id); validates the category exists, pre-checks SKU uniqueness (equality filter on the `HasConversion`-mapped `Sku`), then `Product.Create` with a constructed `Sku`/`Money`/`VatPercentage` |
| `api/v{version}/product/{id}` | PUT | `catalog.products.manage` | `UpdateProductRequest` | `Result`; one general-purpose update (`Rename`+`UpdateDescription`+`Reprice`+`UpdateVatRate`+`Recategorize` together) — `Sku` is immutable post-create |
| `api/v{version}/product/{id}/activate` | PUT | `catalog.products.manage` | Route `id` | `Result`; `Product.Activate` |
| `api/v{version}/product/{id}/deactivate` | PUT | `catalog.products.manage` | Route `id` | `Result`; `Product.Deactivate` |
| `api/v{version}/product/{id}/image` | POST | `catalog.products.manage` | `AddProductImageRequest { Url, SortOrder? }` | `Result`; `Product.AddImage` |
| `api/v{version}/product/{id}/image` | DELETE | `catalog.products.manage` | Query `url` | `Result`; `Product.RemoveImage(url)` removes every image matching the url |

Every action across both controllers dispatches a mediator command/query under
`Application/{Categories,Products}/{Commands,Queries}` — handlers own their `CatalogDbContext` logic
directly, same shape as `Organization`/`LeaveManagement`/`Location`. `ICatalogPricingService` (see
Notable Conventions) is a DI-only seam with no HTTP surface of its own, and currently has no consumer.

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
  length 200, `ParentCategoryId` max length 450.
- **`Products`** — unique index on `Sku`; index on `CategoryId`. FK to `Category` (`CategoryId`) is
  `Restrict`. `Name` max length 200, `Description` max length 2000, `CategoryId` max length 450.
  `Sku` is a converted scalar column (`HasConversion(sku => sku.Value, v => new Sku(v))`, max length
  `Sku.MaxLength` = 100) rather than an owned type. `Price`/`VatRate` are table-split EF owned types
  (same row) — `PriceAmount decimal(18,2)`/`PriceCurrency` (max length 3), `VatRate decimal(5,2)`;
  `Reprice`/`UpdateVatRate` mutate the tracked owned instance in place via `Money.Update`/
  `VatPercentage.Update` rather than reassigning (see Notable Conventions).
- **`ProductImages`** — owned collection (`OwnsMany`) into its own table with a shadow `int Id`
  surrogate key as the sole PK (not composite with `ProductId`) — Sqlite only auto-populates an
  `INTEGER PRIMARY KEY` via its rowid-alias optimization when that column is the sole PK member;
  `ProductId` is a plain indexed FK column instead. `Url` max length 2048.

Both `Category`/`Product` call `entity.ConfigureAuditableEntity()`; `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: false)` — same
as `Organization`/`Location`, neither entity implements `ISoftDelete`.

Query handlers read `AsNoTracking`. `GetCategoryByIdQueryHandler`/`GetCategoryChildrenQueryHandler` use
Mapster's `ProjectToType<T>`; `GetCategoryTreeQueryHandler` loads the full flat table once and
assembles the tree in memory via `GroupBy(x => x.ParentCategoryId ?? string.Empty)` — no recursive
CTE, same approach as Location/Organization's tree endpoints. `GetProductByIdQueryHandler`/
`ListProductsQueryHandler`/`CatalogPricingService` hand-map the materialised `Product` entity to its
DTO instead of an EF `Select` projection — `Sku` is a `HasConversion`-mapped scalar and `Price`/
`VatRate` are owned-type table-split columns that load automatically with the entity, and there is no
established precedent in this repo for composing further member access (`x.Sku.Value`) inside a
server-translated `Select`; loading the entity sidesteps the question (see `CatalogPricingService`'s
class doc).

Migrations exist for **MSSQL only so far**: `src/Migrations/MSSQL/Catalog/` holds a single migration
(`CreateCatalogSchema`) — not yet a squashed baseline (per the dev-migration-squash convention,
squashing happens once a module is judged complete), and it is also the only migration to date, so
there is nothing to squash yet. The `PostgreSQL`/`Sqlite` migration projects do not yet reference
`Catalog.Api` at all. `src/Migrations/MSSQL/Program.cs` calls only
`CatalogContextInitialiser.InitialiseAsync()` — no seed data (`CatalogContextInitialiser` has no
`TrySeedAsync`, unlike Location's).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Catalog.Contracts → Shared`) | `BaseDto` for `CategoryDto`/`CategoryTreeNodeDto`/`ProductDto`/`ProductPriceInfoDto`; the `Money`/`VatPercentage` value objects and `CurrencyConstants.Default` consumed by the `Product` aggregate. |
| `Infrastructure` | project (`Catalog.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Catalog.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity`. |
| `Catalog.Contracts` | project (`Catalog.Api → Catalog.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result`, `Mapster` (`Catalog.Api`) | package, **all declared directly** | Same positive contrast as `Organization`/`Approval`/`LeaveManagement`/`Location` — no undeclared-transitive-dependency instance. |

`Catalog` has **no outgoing dependency on any other business module** — like `Location`, it reaches no
other module's `Contracts` seam.

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/MSSQL` — references `Catalog.Api` directly for `CatalogDbContext`/
  `CatalogContextInitialiser`. `PostgreSQL`/`Sqlite` do not (see Data Access).
- `Catalog.Tests` — `Catalog.Api.csproj` grants `InternalsVisibleTo` to reach the `internal`
  command/query records and handlers.

Nothing currently references `Catalog.Contracts` to consume `ICatalogPricingService` — it is built
ahead of any real consumer, mirroring the role `ILocationDirectoryService` plays for `Location` (its
own XML doc names Orders/Inventory as candidate future consumers, neither of which exist yet).

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
- **`Money`/`VatPercentage` are genuinely shared-kernel value objects** (`src/Shared/ValueObjects/`),
  not Catalog-specific — `Product` is their first and only consumer today. Both guard construction and
  expose an `internal Update` that mutates the tracked instance in place rather than being reassigned
  (same `DateRange.Update`/`ActiveStatus.Update` pattern `LeaveManagement` established) — reassigning
  an owned reference makes EF's change tracker emit the old instance as `Deleted`, which
  `TrackingExtensions.AuditEntries` resets back to `Unchanged` to guard against nulling the owned
  columns, but that reset then leaves the new values unpersisted. Since they live in a different
  assembly than `Catalog.Api`, `Shared.csproj` grants a scoped
  `InternalsVisibleTo("StarterKit.Catalog.Api")` to reach `Update`, alongside its existing
  `InternalsVisibleTo("Framework.Tests")`. See
  [../architecture.md § Shared Kernel](../architecture.md#shared-kernel--common-building-blocks).
- **`Sku` is a converted-scalar value object (`HasConversion`), not an owned type** — deliberately does
  not derive from `Light.Domain.ValueObjects.ValueObject` (that base exists to survive the
  tracked-owned-type `Deleted`/`Added` replace hazard described above, which only applies to owned
  navigations, not a converted scalar). Uniqueness is a handler pre-check plus a DB unique index, same
  belt-and-suspenders split as every other uniqueness rule in this repo.
- **`ProductImageUrl` is an owned collection (`OwnsMany`) with no in-place `Update`** — unlike `Money`/
  `VatPercentage`'s single table-split references, a collection member is always fully added or
  removed via `Product.AddImage`/`RemoveImage`, so it never needs one.
- **Domain aggregates hold only real domain rules — not input-shape validation** — the same
  project-wide convention `Location` established (see
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md)).
  `CreateCategoryCommandHandler`'s parent-existence/name-uniqueness checks and
  `CreateProductCommandHandler`'s category-existence/SKU-uniqueness checks are handler-level guards,
  not domain invariants.
- **FluentValidation, two layers** — the same convention `Location` introduced (see
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md)): each `Contracts`
  request DTO carries an `AbstractValidator<TRequest>` in the same file (field-shape rules only); each
  mediator command has a thin `AbstractValidator<TCommand>` (same file as the command+handler)
  validating the route-level `Id` (`NotEmpty`) directly and delegating to the Contracts validator via
  `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`.
- **`CatalogPermissions` has only `View`/`Manage`, not the four-way `View`/`Create`/`Update`/`Delete`
  split most other modules use** — mirrors `Location`'s per-module simplification.
- **`ICatalogPricingService`/`CatalogPricingService` is the module's cross-module DI seam**, same role
  as Location's `ILocationDirectoryService` — `GetPriceInfoAsync`/`GetPriceInfoBatchAsync`, both
  read-only and `AsNoTracking`. Currently has **zero consumers**; it exists ahead of a future
  Orders/Inventory-style module (named directly in its own XML doc).
- **Migration is MSSQL-only, a single unsquashed migration** — see Data Access. Treat `Catalog` as
  mid-development, not yet at the "template baseline" state `Organization`/`Approval`/
  `LeaveManagement` are in.
- `Specification<T>` (vendor `Light.Specification`) is used only for the by-id lookups
  (`CategoryByIdSpec`, `ProductByIdSpec`), reused across several handlers each — same policy as
  Organization's/Location's.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-12_
