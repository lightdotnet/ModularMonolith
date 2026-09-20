# Module Overview: Location

## Purpose

Owns a self-referencing physical-location hierarchy (Store/Warehouse/Terminal/Bin, or any other
data-driven type) via two aggregates. `Location` is a single self-referencing entity — the node in
the hierarchy — whose allowed-parent-type rule is not hardcoded but resolved from `LocationType`,
a small, admin-configurable catalog table that replaced an original hardcoded enum design mid-session
(explicit user request: management must not depend on a code change to add a new location type).
Each `LocationType` row declares its own `AllowedParentTypeId` (nullable self-FK; `null` = must be a
root location) and `CanHaveChildren`; unlike every other identifier in this solution, `LocationType.Id`
is a caller-supplied business code (e.g. `"STORE"`), not framework-generated. The module also exposes
`ILocationDirectoryService`, a read-only cross-module seam (same role as Organization's
`IOrgDirectoryService`) for another module to resolve location data without reaching into this module's
aggregate or EF internals — it now has two consumers, `Orders` (order-creation `LocationId` validation)
and `Inventory` (stock-adjustment `LocationId` validation), see Notable Conventions.

## Internal Layering

Location is a **single-project module** (not split Domain/Application/Infrastructure/Api), following
the same structural convention as `Organization`/`Approval`/`LeaveManagement`:

| Project | Responsibility | Notes |
|---|---|---|
| `Location.Contracts` | DTOs, requests, enums, and the permission catalog, organized into per-feature subfolders — `Common/` (`LocationStatus`: `Active`/`Inactive`, `LocationTypeStatus`: `Active`/`Inactive`), `Locations/` (`LocationDto`, `LocationTreeNodeDto` (adds a `Children` list), `LocationLookupDto` (thin picker projection — `Id`/`Name`/`Code`/`LocationTypeId` only), `CreateLocationRequest`, `UpdateLocationRequest`, `MoveLocationRequest`), `LocationTypes/` (`LocationTypeDto`, `CreateLocationTypeRequest`, `UpdateLocationTypeRequest`), `Services/` (`ILocationDirectoryService` — the module's cross-module seam, see Notable Conventions), `Authorization/` (`LocationPermissions`, `LocationPermissionProvider`). Every Request DTO carries its own `AbstractValidator<TRequest>` **in the same file** — see Notable Conventions for the two-layer FluentValidation convention this module introduces to the repo. Declares `Lightsoft.AspNetCore.Authorization` directly. |
| `Location.Api` | Single project organized by folder: `Domain/{Locations,LocationTypes}` (`Domain/Locations/Location.cs` + `LocationByIdSpec.cs`, `Domain/LocationTypes/LocationType.cs` + `LocationTypeByIdSpec.cs` — see Notable Conventions for the `Specification<T>` pattern), `Data/` (`LocationDbContext`, `LocationContextInitialiser`), `Application/{Locations,LocationTypes}/{Commands,Queries}` (every handler owns its business logic directly against `LocationDbContext` — no service-class indirection — plus a thin per-command `AbstractValidator`, see Notable Conventions), `Services/` (`ILocationTypeCache`/`LocationTypeCache`, both `internal` — a module-local full-table read cache, see Notable Conventions; `LocationDirectoryService`, `internal`, implementing `ILocationDirectoryService`), `Controllers/` (`LocationController`, `LocationTypeController`), `LocationModule.cs` (DI: DbContext + `ILocationDirectoryService` + `ILocationTypeCache` + permission provider). | `RootNamespace`/`AssemblyName` are set to the plural `StarterKit.Locations.Api`/`StarterKit.Locations.Contracts` (project/folder names stay singular `Location.Api`/`Location.Contracts`) — see Notable Conventions for why. |

## Public Contract

`LocationController` (route `location`, `[MustHavePermission(LocationPermissions.Locations.View)]` at
class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/location/tree` | GET | `location.locations.view` | — | `IList<LocationTreeNodeDto>` — flat table loaded once, assembled into a tree in memory by grouping on `ParentLocationId` (same pattern as Organization's org-unit tree) |
| `api/v{version}/location/{id}` | GET | `location.locations.view` | Route `id` | `Result<LocationDto>` |
| `api/v{version}/location/{id}/children` | GET | `location.locations.view` | Route `id` | `IList<LocationLookupDto>` — immediate children only, thin lookup projection |
| `api/v{version}/location` | POST | `location.locations.manage` | `CreateLocationRequest` | `Result<string>` (new id); resolves the `LocationType` via `ILocationTypeCache`, resolves an optional parent, then `Location.Create` re-validates the allowed-parent-type/`CanHaveChildren` rule; rejects a duplicate `Code` |
| `api/v{version}/location/{id}` | PUT | `location.locations.manage` | `UpdateLocationRequest` | `Result`; edits `Name`/`Code`/`Status` only — `Type` and parent are immutable here, reparenting goes through Move; rejects a `Code` collision |
| `api/v{version}/location/{id}/move` | PUT | `location.locations.manage` | `MoveLocationRequest { NewParentLocationId }` | `Result`; guards: can't parent to itself, new parent must exist, re-validates the allowed-parent-type rule against the new parent via `Location.Move`, and can't move under one of its own descendants (walked iteratively via `ParentLocationId`, one round-trip per ancestor level — same pattern as Organization's `MoveOrgUnitCommandHandler`) |
| `api/v{version}/location/{id}` | DELETE | `location.locations.manage` | Route `id` | `Result`; blocked if the location still has child locations |

`LocationTypeController` (route `location_type`,
`[MustHavePermission(LocationPermissions.LocationTypes.View)]` at class level):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/location_type` | GET | `location.location_types.view` | — | `IList<LocationTypeDto>`, ordered by `Name` |
| `api/v{version}/location_type/{id}` | GET | `location.location_types.view` | Route `id` | `Result<LocationTypeDto>` |
| `api/v{version}/location_type` | POST | `location.location_types.manage` | `CreateLocationTypeRequest { Id, Name, AllowedParentTypeId?, CanHaveChildren }` | `Result<string>`; `Id` is **caller-supplied**, not generated — rejects a duplicate id, validates `AllowedParentTypeId` references an existing type if given; reloads `ILocationTypeCache` after commit |
| `api/v{version}/location_type/{id}` | PUT | `location.location_types.manage` | `UpdateLocationTypeRequest` | `Result`; rejects a self-referencing `AllowedParentTypeId`, validates it references an existing type if given; reloads `ILocationTypeCache` after commit |
| `api/v{version}/location_type/{id}` | DELETE | `location.location_types.manage` | Route `id` | `Result`; blocked if any `Location` still uses this type, or another `LocationType` still references it as its `AllowedParentTypeId`; reloads `ILocationTypeCache` after commit |

Every action across both controllers dispatches a mediator command/query under
`Application/{Locations,LocationTypes}/{Commands,Queries}` — handlers own their `LocationDbContext`
logic directly, same shape as `Organization`/`LeaveManagement`. `ILocationDirectoryService` (see
Notable Conventions) is a DI-only seam with no HTTP surface of its own, consumed by `Orders` and
`Inventory`.

`LocationPermissions.{Locations,LocationTypes}` each expose only `View`/`Manage` — **not** the
`View`/`Create`/`Update`/`Delete` four-way split every other module's permission catalog uses. A
deliberate per-module simplification (explicit user request), not an oversight — see Notable
Conventions.

## Data Access

`LocationDbContext : BaseDbContext`, schema `"location"`, registered via
`Persistence.DbContextExtensions.AddConfiguredDbContext<LocationDbContext>(configuration, DbConnectionNames.Location)`.
`DbConnectionNames.Location` aliases `DbConnectionNames.Default` ("DefaultConnection") — same physical
database/connection string as every other module, separated only by schema (`location`) + table name.

Two tables:

- **`Locations`** — unique index on `Code`; index on `ParentLocationId`; index on `LocationTypeId`.
  Self-referencing `Parent`/`Children` FK (`ParentLocationId`) is `DeleteBehavior.Restrict`; FK to
  `LocationType` (`LocationTypeId`) is also `Restrict`. `Name` max length 200, `Code` max length 50,
  `LocationTypeId`/`ParentLocationId` max length 450.
- **`LocationTypes`** — index on `AllowedParentTypeId`; self-referencing FK to itself
  (`AllowedParentTypeId`, no navigation property) is `Restrict`. `Id` is configured
  `ValueGeneratedNever()` — it is a caller-supplied business code, not a framework-generated id (see
  Notable Conventions). `Name` max length 200, `AllowedParentTypeId` max length 450.

Both entities call `entity.ConfigureAuditableEntity()`; `SaveChanges[Async]` calls
`TrackingExtensions.AuditEntries(currentUser.UserId, clock.AuditTime, enableSoftDelete: false)` — same
as `Organization`, neither entity implements `ISoftDelete`, so this is not a comparable soft-delete
gap, just no soft-delete support at all in this module.

Query handlers read `AsNoTracking`. `GetLocationByIdQueryHandler`/`GetLocationTypeByIdQueryHandler`/
`GetLocationTypesQueryHandler` use Mapster's `ProjectToType<T>`; `GetLocationChildrenQueryHandler` and
`LocationDirectoryService` use an explicit `.Select` into `LocationLookupDto`;
`GetLocationTreeQueryHandler` loads the full flat table once and assembles the tree in memory via
`GroupBy(x => x.ParentLocationId ?? string.Empty)` — no recursive CTE, same approach as Organization's
`org_unit` tree endpoint.

Migrations exist for **MSSQL only so far**: `src/Migrations/MSSQL/Location/` holds two incremental
migrations (`CreateLocationSchema`, then `AddLocationTypeEntity`) — **not yet squashed to a single
baseline**, unlike `Organization`/`Approval`/`LeaveManagement`'s squashed baselines, because this
module is still mid-development (per the dev-migration-squash convention, squashing happens once a
module is judged complete). The `PostgreSQL`/`Sqlite` migration projects do not yet reference
`Location.Api` at all. `src/Migrations/MSSQL/Program.cs` calls
`LocationContextInitialiser.InitialiseAsync()` then `TrySeedAsync()`, seeding the four location types
that reproduce the pre-refactor hardcoded rules exactly: `STORE`/`WAREHOUSE` as roots that can have
children, `TERMINAL` (must be parented under `STORE`, always a leaf), `BIN` (must be parented under
`WAREHOUSE`, always a leaf). Seeding is idempotent, checked by `Id` before inserting
(`GetOrCreateLocationTypeAsync`).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Location.Contracts → Shared`) | `BaseDto` for `LocationDto`/`LocationTreeNodeDto`/`LocationLookupDto`/`LocationTypeDto`. |
| `Infrastructure` | project (`Location.Api → Infrastructure`) | `VersionedApiController`, `AppModule` base class. |
| `Persistence` | project (`Location.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, `AuditEntries`/`ConfigureAuditableEntity`. |
| `Location.Contracts` | project (`Location.Api → Location.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.Caching`, `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result`, `Mapster` (`Location.Api`) | package, **all declared directly** | Same positive contrast as `Organization`/`Approval`/`LeaveManagement` — no undeclared-transitive-dependency instance. `Lightsoft.Caching` backs `ILocationTypeCache`'s `Light.Extensions.Caching.ICacheService` dependency. |

`Location` has **no outgoing dependency on any other business module** — unlike `Organization`/
`Approval`/`LeaveManagement`, it reaches no other module's `Contracts` seam.

## Depended On By

- `StarterKit.WebApi` — composition-root host (wired into `ConfigureExtensions.cs`'s `assemblies`
  array).
- `src/Migrations/MSSQL` — references `Location.Api` directly for `LocationDbContext`/
  `LocationContextInitialiser`. `PostgreSQL`/`Sqlite` do not (see Data Access).
- `Location.Tests` — `Location.Api.csproj` grants `InternalsVisibleTo` to reach the `internal`
  command/query records and handlers, plus a second grant to `DynamicProxyGenAssembly2` (see Notable
  Conventions).
- **`Orders.Api`** — references `Location.Contracts`, consumed by `CreateOrderCommandHandler` via
  `ILocationDirectoryService.ExistsAsync` to validate `LocationId` before creating an order (see
  [Orders.md](Orders.md)).
- **`Inventory.Api`** — references `Location.Contracts`, consumed by `InventoryService
  .DecrementForOrderAsync` and `RecordStockMovementCommandHandler` via `ILocationDirectoryService
  .ExistsAsync` to validate `LocationId` before any stock movement (see [Inventory.md](Inventory.md)).

`ILocationDirectoryService` now has two real cross-module consumers, `Orders` (first) and `Inventory`
(second) — the same role `IOrgDirectoryService` plays for `Organization`/`LeaveManagement`.

## Notable Conventions

- **`LocationType` is a data-driven replacement for a hardcoded enum, adopted mid-session on explicit
  user request** ("quản lý không phụ thuộc vào code" — management must not depend on a code change).
  Each row declares its own `AllowedParentTypeId` (nullable self-FK; `null` = must be root) and
  `CanHaveChildren`; `Location.ValidateParent` reads the resolved `LocationType`'s data instead of a
  hardcoded switch statement. `LocationType.Id` is a **caller-supplied business code**
  (e.g. `"STORE"`), not framework-generated — the only entity in this solution configured
  `ValueGeneratedNever()`.
- **Domain aggregates hold only real domain rules — not input-shape validation.** `Location.Create`/
  `Update`/`Move` and `LocationType.Create`/`Update` validate only genuine invariants (the
  allowed-parent-type/`CanHaveChildren` rule); required/length/enum-range checks were deliberately
  moved entirely to FluentValidation, per explicit mid-session user instruction. This is now a
  **project-wide convention**, not a `Location`-specific choice — see
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md).
- **FluentValidation, two layers — the first real per-module usage of FluentValidation in this repo.**
  The `ValidationBehaviour<,>` pipeline behavior and `AddValidatorsFromAssemblies` wiring
  (`StarterKit.WebApi/ConfigureExtensions.cs`) already existed, but no module had an actual validator
  registered before `Location`. Contracts-level: each Request DTO (`CreateLocationRequest`,
  `UpdateLocationRequest`, `MoveLocationRequest`, `CreateLocationTypeRequest`,
  `UpdateLocationTypeRequest`) has an `AbstractValidator<TRequest>` in the same file, holding all
  field-shape rules (`NotEmpty`, `MaximumLength`, `IsInEnum`). Api-level: each mediator command has a
  thin `AbstractValidator<TCommand>` (same file as the command+handler) that validates the route-level
  `Id` (`NotEmpty`) directly and delegates to the Contracts validator via
  `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. See
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md).
- **`ILocationTypeCache`/`LocationTypeCache` is an internal, module-local, hand-written full-table
  cache** over `LocationType` (`Light.Extensions.Caching.ICacheService`, cache key
  `"location:location-types:all"`), invalidated purely by being overwritten on every
  `LocationType` Create/Update/Delete (`ReloadAsync`, called post-commit in each handler) — no
  time-based expiration. **Deliberately not built on the new opt-in `ICacheRepository<T>` from
  `Persistence/Repositories`** (see [../architecture.md § Shared Kernel](../architecture.md#shared-kernel--common-building-blocks)) — reviewed and kept as-is because it
  already avoids that repository's unenforced write-path-bypass hazard by construction (explicit
  `ReloadAsync` calls, never touches `SaveChanges` on the cached entity directly).
- **`LocationPermissions` has only `View`/`Manage`, not the four-way `View`/`Create`/`Update`/`Delete`
  split every other module uses.** A deliberate per-module simplification the user requested for this
  module — an intentional inconsistency across the codebase, not a gap to "fix" elsewhere.
- **Namespace quirk: plural `RootNamespace`/`AssemblyName`.** `Location.Api.csproj`/
  `Location.Contracts.csproj` set `RootNamespace`/`AssemblyName` to `StarterKit.Locations.Api`/
  `StarterKit.Locations.Contracts` (project/folder names stay singular `Location.Api`/
  `Location.Contracts`) — a type literally named `Location` collides with a namespace segment named
  `Location` (`CS0118`) anywhere under a `StarterKit.Location.*` tree, and the plural namespace avoids
  it. `LocationType` has no such collision.
- **First place in the repo needing `InternalsVisibleTo("DynamicProxyGenAssembly2")`.**
  `Location.Api.csproj` declares it (alongside the usual `InternalsVisibleTo("Location.Tests")`) —
  required for Moq's Castle DynamicProxy to mock the `internal interface ILocationTypeCache`; no other
  module's tests mock an internal interface with Moq yet. See
  [../../conventions/coding-conventions.md](../../conventions/coding-conventions.md) for the
  documented pattern.
- **`ILocationDirectoryService`/`LocationDirectoryService` is the module's cross-module DI seam**,
  same role as Organization's `IOrgDirectoryService` — `GetAsync`/`ExistsAsync`/`GetChildrenAsync`/
  `GetLookupAsync`, all read-only and `AsNoTracking`. It now has two real consumers: `Orders`
  (`CreateOrderCommandHandler`, validating `LocationId` on order creation) and `Inventory`
  (`InventoryService.DecrementForOrderAsync` and `RecordStockMovementCommandHandler`, validating
  `LocationId` on every stock movement) — see Depended On By.
- **Migration is MSSQL-only and not yet squashed** — see Data Access. Treat `Location` as
  mid-development, not yet at the "template baseline" state `Organization`/`Approval`/
  `LeaveManagement` are in.
- `Specification<T>` (vendor `Light.Specification`) is used only for the by-id lookups
  (`LocationByIdSpec`, `LocationTypeByIdSpec`), reused across several handlers each — same policy as
  Organization's.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-20_
