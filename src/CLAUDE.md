# Backend — StarterKit.WebApi

Project-specific guidance for the backend solution under `src/`. See the root [CLAUDE.md](../CLAUDE.md) for repository-wide rules (language convention, code-change workflow gate, agent/skill/workflow usage) — this file only covers what's specific to the backend.

## Purpose

ASP.NET Core (C#) **Modular Monolith** — one solution (`StarterKit.slnx`). `src/StarterKit.WebApi` is the primary deployable process; `src/Identity.Web` (the Identity module's Razor Pages login host) is co-hosted there and can also run standalone as a login-only host. Built on the private `Lightsoft.*` (`Light.*`) vendor framework family (mediator, `Result`/`Paged` contracts, domain base types, ASP.NET Core authorization/modularity/CORS helpers, EF Core helpers, caching, Serilog).

## Stack

`net10.0`, EF Core (provider-configurable: `InMemory`/`PostgreSQL`/`MSSQL`/`Sqlite` via `DbProvider`), xUnit v3 for tests, central package management via `Directory.Packages.props`.

## Modules

| Module | Path | Responsibility |
|---|---|---|
| Identity | `src/Identity.Api` + `src/Identity.Contracts` + `src/Identity.Web` | Users, roles, claims; API token issuance (password/AD) + refresh + sessions; interactive cookie login and Microsoft Entra ID (OIDC) external login (`Identity.Web`), also relayed to separate-origin clients via a one-time PKCE code exchange (see `Identity.Web/Pages/Account/ExternalLoginStart.cshtml.cs`); SignalR hub handshake token; permission catalog. |
| Notifications | `src/Notifications.Api` + `src/Notifications.Contracts` | Notification storage + real-time push over SignalR (admin + self-service surfaces); owns the welcome-mail handlers reacting to Identity's integration events. |
| Organization | `src/Organization.Api` + `src/Organization.Contracts` | Companies, department/team hierarchy (`OrgUnit`), company-scoped employee levels, employees, and optional employee-to-Identity-login linking. Also exposes `IOrgDirectoryService`, a cross-module seam consumed by `LeaveManagement`. |
| Approval | `src/Approval.Api` + `src/Approval.Contracts` | Generic, reusable multi-level approval-request engine — the calling module resolves the approver chain and drives the workflow via `IApprovalService`; not tied to any specific request type. |
| LeaveManagement | `src/LeaveManagement.Api` + `src/LeaveManagement.Contracts` | Self-service CRUD for employee leave requests — delegates the actual approval workflow entirely to `Approval` via `IApprovalService` and resolves approvers/display names via `Organization`'s `IOrgDirectoryService`; has no decide/approve endpoint of its own. |
| Location | `src/Location.Api` + `src/Location.Contracts` | Self-referencing physical-location hierarchy (`Location`: Store/Warehouse/Terminal/Bin or any other data-driven type) plus a data-driven `LocationType` catalog replacing what would otherwise be a hardcoded enum — each type declares its own allowed-parent-type rule. Also exposes `ILocationDirectoryService`, a cross-module seam with no current consumers (built ahead of a future Catalog/Orders/Inventory-style module). |
| Catalog | `src/Catalog.Api` + `src/Catalog.Contracts` | Self-referencing product-category tree (`Category`) plus `Product` CRUD/search/activate-deactivate/image management, priced via the shared `Money`/`VatPercentage` value objects. Also exposes `ICatalogPricingService`, a cross-module seam with no current consumers (built ahead of a future Orders/Inventory-style module). |
| Orders | `src/Orders.Api` + `src/Orders.Contracts` | Owns the sale lifecycle — draft cart through placement, payment reconciliation, fulfillment, or cancellation — via the `Order` aggregate and a separate `Payment` aggregate sharing one `OrdersDbContext`, plus a data-driven `OrderType` catalog (fee/payment types). Consumes `Catalog`'s `ICatalogPricingService` and `Location`'s `ILocationDirectoryService` — the first real cross-module consumer of either seam — plus `Inventory`'s `IInventoryService`, called synchronously (not via an integration event) to decrement/restore stock on placement/cancellation. |
| Inventory | `src/Inventory.Api` + `src/Inventory.Contracts` | Tracks on-hand stock per product per location via a `StockLevel` running total and an immutable `StockAdjustment` ledger, coordinated in one commit by the internal `StockLedger` domain service; enforces strict no-oversell. Consumes `Location`'s `ILocationDirectoryService` (its second consumer, after Orders) and exposes `IInventoryService`, `Orders`' synchronous cross-module seam for stock movement. |

Plus shared/host projects: `src/Shared` (shared kernel, leaf), `src/Infrastructure` (cross-cutting infra), `src/Persistence` (EF Core concerns), `src/StarterKit.WebApi` (composition-root host).

## Design Approach

Backend design is **DDD-first**: model the domain deliberately before writing handlers — aggregate boundaries and invariants, entity vs. value object, domain events for cross-aggregate/cross-module reactions, and business rules living on the aggregate/entity rather than in a `CommandHandler` or service (an anemic model is a smell). `Organization`/`Approval`/`LeaveManagement`/`Location`/`Catalog`/`Orders`'s `Domain/<Feature>/` folders are the pattern; each module doc's § Notable Conventions has the specifics. Stay pragmatic — plain enums where a value object adds nothing, `Light.Specification` only for reused/special-case predicates. Input-shape validation (required/length/enum-range checks) is not a domain rule — it belongs in FluentValidation on the incoming request, not the aggregate; `Location`'s `Domain/` is the reference for this split (see [docs/conventions/coding-conventions.md](docs/conventions/coding-conventions.md)).

## Architectural Constraints ("do not" rules)

- **Every module's `<Module>.Contracts` project is the only seam other modules or the host may reference.** Never reference another module's internals (its single-project folders, or a split module's `Domain`/`Application`/`Infrastructure`/`Api`) directly. (`Identity.Web → Identity.Api` is allowed — both are the same module.)
- **One `DbContext` per module is the default**, even when modules share one physical database (current state: `Identity`/`Notifications`/`Organization`/`Approval`/`LeaveManagement`/`Location`/`Catalog`/`Orders`/`Inventory` share one DB, separated by schema/table).
- Full module structure convention (single-project vs. Clean-Architecture split, `.Api`-suffix naming) — see [docs/architecture/architecture.md § Layering](docs/architecture/architecture.md#layering).

## Architecture

- [docs/architecture/overview.md](docs/architecture/overview.md) — solution overview, dependency graph summary, entry points.
- [docs/architecture/architecture.md](docs/architecture/architecture.md) — layering, dependency direction, key design patterns, shared kernel.
- [docs/architecture/dependency-graph.md](docs/architecture/dependency-graph.md) — package references, circular-reference/boundary-violation check.
- [docs/architecture/modules/Identity.md](docs/architecture/modules/Identity.md) / [docs/architecture/modules/Notifications.md](docs/architecture/modules/Notifications.md) / [docs/architecture/modules/Organization.md](docs/architecture/modules/Organization.md) / [docs/architecture/modules/Approval.md](docs/architecture/modules/Approval.md) / [docs/architecture/modules/LeaveManagement.md](docs/architecture/modules/LeaveManagement.md) / [docs/architecture/modules/Location.md](docs/architecture/modules/Location.md) / [docs/architecture/modules/Catalog.md](docs/architecture/modules/Catalog.md) / [docs/architecture/modules/Orders.md](docs/architecture/modules/Orders.md) / [docs/architecture/modules/Inventory.md](docs/architecture/modules/Inventory.md) — per-module deep dive.

## Conventions

- [docs/conventions/coding-conventions.md](docs/conventions/coding-conventions.md) — build/tooling, style, structural/testing conventions.
- [docs/conventions/development-guide.md](docs/conventions/development-guide.md) — local setup, common tasks, where to look for X.
- [docs/conventions/docker-cli.md](docs/conventions/docker-cli.md) — local Postgres/Redis/pgAdmin via Docker.
- [docs/conventions/migrations.md](docs/conventions/migrations.md) — EF Core migration CLI cheat sheet.

## Testing

```bash
dotnet test tests/Framework.Tests/Framework.Tests.csproj
dotnet test tests/Identity.Tests/Identity.Tests.csproj
dotnet test tests/Organization.Tests/Organization.Tests.csproj
dotnet test tests/Approval.Tests/Approval.Tests.csproj
dotnet test tests/LeaveManagement.Tests/LeaveManagement.Tests.csproj
dotnet test tests/Location.Tests/Location.Tests.csproj
dotnet test tests/Catalog.Tests/Catalog.Tests.csproj
dotnet test tests/Orders.Tests/Orders.Tests.csproj
dotnet test tests/Inventory.Tests/Inventory.Tests.csproj
```

xUnit v3 runs on Microsoft.Testing.Platform. On the .NET 10 SDK `dotnet test` refuses the legacy VSTest path — if it errors with "opt-in to the new dotnet test experience", run the built test executable directly instead (`tests/<Name>/bin/Debug/net10.0/<Name>.exe`, filters: `-class <FQN>` / `-method <FQN>`).

`Notifications` has no dedicated test project yet (see Known Debt).

## Known Debt

See [docs/known-debt.md](docs/known-debt.md) — the single, current-state-only list of open backend technical debt/pending architecture decisions. Update it directly when debt is found or resolved; don't re-scatter items back into the architecture docs above.

---
_Last synced: 2026-09-20_
