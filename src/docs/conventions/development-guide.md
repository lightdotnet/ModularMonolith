# Development Guide: Backend

## Prerequisites

- .NET SDK `10.0.301` (or compatible `net10.0`-targeting SDK).
- A reachable database matching the configured `DbProvider` (see Local Setup below) if running the host — no external service is required just to run the test suite.

## Building

```
dotnet build StarterKit.slnx
```

## Running Locally

```
dotnet run --project src/StarterKit.WebApi/StarterKit.WebApi.csproj
```

The default `http` launch profile binds plain HTTP on `http://localhost:5000`, which is the primary Development endpoint — `Program.cs` skips `UseHttpsRedirection()` when `IsDevelopment()`, so no HTTPS listener is needed for local work (this also lets a server-to-server client such as the Next.js admin BFF call the API without hitting the untrusted dev certificate). The `https` profile additionally binds `https://localhost:5001` (ASP.NET dev certificate) for anyone who wants it; outside Development the HTTPS redirect is active as normal.

`src/StarterKit.WebApi/Program.cs` boots the host: configures Serilog, calls `ConfigureServices`, wires MVC/JSON options, then `ConfigurePipelines()` + `MapEndpoints(...)`. By default (`appsettings.json`) `DbProvider` is `MSSQL`, pointing `ConnectionStrings:DefaultConnection` at a local `(localdb)\mssqllocaldb` instance — switch to `InMemory`/`Sqlite`/`PostgreSQL` via `IConfiguration["DbProvider"]` (and matching `ConnectionStrings:DefaultConnection`, commented-out examples for PostgreSQL/Sqlite are already present in `appsettings.json`) if you don't have SQL Server LocalDB available. `AllowAnonymous` is `false` by default in `appsettings.json` (`appsettings.Development.json` does not override it) — requests need a valid JWT unless the endpoint is explicitly anonymous.

## Running Tests

```
dotnet test tests/Framework.Tests/Framework.Tests.csproj
dotnet test tests/Identity.Tests/Identity.Tests.csproj
dotnet test tests/Organization.Tests/Organization.Tests.csproj
dotnet test tests/Approval.Tests/Approval.Tests.csproj
dotnet test tests/LeaveManagement.Tests/LeaveManagement.Tests.csproj
dotnet test tests/Location.Tests/Location.Tests.csproj
dotnet test tests/Catalog.Tests/Catalog.Tests.csproj
dotnet test tests/Orders.Tests/Orders.Tests.csproj
```

No special setup needed — all current tests are unit tests using EF Core's InMemory/Sqlite providers directly (no external database/services required). `Framework.Tests` covers `Shared`, `Infrastructure`, and `Persistence`; each module's own `<Module>.Tests` project covers that module's `<Module>.Api`. `Notifications` has no dedicated test project yet.

## Local Setup

- Default `DbProvider` is `MSSQL` (`src/StarterKit.WebApi/appsettings.json`) with a `(localdb)\mssqllocaldb` connection string checked into `appsettings.json` as a starter-template default — replace for real use, do not treat as a production secret.
- JWT signing (`Jwt:SecretKey`, `Jwt:Issuer`, token lifetimes) and Basic Auth (`BasicAuth: "super:123"`) values in `appsettings.json` are template placeholders, not production secrets — replace before any real deployment.
- `UserSecretsId` is set on `StarterKit.WebApi.csproj` for local `dotnet user-secrets` overrides if preferred over editing `appsettings.Development.json` directly.

## Common Tasks

| Task | How |
|---|---|
| Add a backend migration | `dotnet ef migrations add <Name> --project src/Migrations/<Provider>/<Provider>.csproj --context IdentityDbContext --output-dir Identity` where `<Provider>` is `MSSQL`, `PostgreSQL`, or `Sqlite` — run once per provider (design-time EF migration projects live under top-level `src/Migrations/{MSSQL,PostgreSQL,Sqlite}`). MSSQL currently has a single baseline migration (`CreateIdentitySchema`); Sqlite/PostgreSQL each have that same baseline plus one incremental migration (`AddUserCreatedIndex`) on top of it. Full EF CLI cheat sheet: [migrations.md](migrations.md). |
| Run local infra (Postgres, Redis, pgAdmin) via Docker | See [docker-cli.md](docker-cli.md). |
| Run the API locally | `dotnet run --project src/StarterKit.WebApi/StarterKit.WebApi.csproj` |
| Run the backend test suite | Run each module's `dotnet test tests/<Module>.Tests/<Module>.Tests.csproj` in turn — see § Running Tests. |
| Build the whole solution | `dotnet build StarterKit.slnx` |

## Where to Look for X

- Shared abstractions/base types (`ICurrentUser`, `IDateTime`, `Status`, `BaseDto`, entity wrappers, authorization): `src/Shared/`.
- Cross-cutting infrastructure (CORS, health checks, Serilog bootstrap logging, Mapster config, module/endpoint base classes): `src/Infrastructure/`.
- EF Core provider config, DbContext base class, audit/soft-delete tracking, domain-event dispatch: `src/Persistence/`.
- Identity module (users, roles, claims, JWT auth): `src/Identity.Api/` (entities in `Entities/`, DbContext in `Data/`, services in `Services/` + `Jwt/`, controllers in `Controllers/`); public DTOs/service interfaces in `src/Identity.Contracts/`. Deep-dive doc: [../architecture/modules/Identity.md](../architecture/modules/Identity.md).
- Notifications module (notification storage + real-time push): `src/Notifications.Api/` (entities in `Entities/`, DbContext in `Data/`, service in `Services/`, controllers in `Controllers/`, SignalR hub in `SignalR/`); public DTOs/service interface/permissions in `src/Notifications.Contracts/`. Deep-dive doc: [../architecture/modules/Notifications.md](../architecture/modules/Notifications.md).
- Organization module (companies, org units, employees, employee-login linking): `src/Organization.Api/` (domain in `Domain/<Feature>/`, DbContext in `Data/`, CQRS handlers in `Application/<Feature>/{Commands,Queries}`, controllers in `Controllers/`); public DTOs/`IOrgDirectoryService` in `src/Organization.Contracts/`. Deep-dive doc: [../architecture/modules/Organization.md](../architecture/modules/Organization.md).
- Approval module (generic multi-level approval engine): `src/Approval.Api/` (the `ApprovalRequest` aggregate in `Domain/`, DbContext in `Data/`, `IApprovalService` coordinator + CQRS handlers in `Application/`, controllers in `Controllers/`); public DTOs/`IApprovalService`/integration events in `src/Approval.Contracts/`. Deep-dive doc: [../architecture/modules/Approval.md](../architecture/modules/Approval.md).
- LeaveManagement module (self-service leave requests, delegates approval to Approval): `src/LeaveManagement.Api/` (the `LeaveRequest` aggregate in `Domain/`, DbContext in `Data/`, `LeaveRequestApprovalCoordinator` + CQRS handlers in `Application/`, the `LeaveRequestReconciliationService` background sweep, controllers in `Controllers/`); public DTOs in `src/LeaveManagement.Contracts/`. Deep-dive doc: [../architecture/modules/LeaveManagement.md](../architecture/modules/LeaveManagement.md).
- Location module (self-referencing location hierarchy + data-driven `LocationType` catalog): `src/Location.Api/` (domain in `Domain/{Locations,LocationTypes}/`, DbContext in `Data/`, CQRS handlers in `Application/<Feature>/{Commands,Queries}`, controllers in `Controllers/`); public DTOs/`ILocationDirectoryService` in `src/Location.Contracts/`. Deep-dive doc: [../architecture/modules/Location.md](../architecture/modules/Location.md).
- Catalog module (product-category tree + `Product` CRUD/search/pricing): `src/Catalog.Api/` (domain in `Domain/{Categories,Products}/`, DbContext in `Data/`, CQRS handlers in `Application/<Feature>/{Commands,Queries}`, controllers in `Controllers/`); public DTOs/`ICatalogPricingService` in `src/Catalog.Contracts/`. Deep-dive doc: [../architecture/modules/Catalog.md](../architecture/modules/Catalog.md).
- Orders module (draft-cart-to-sale lifecycle + payment reconciliation): `src/Orders.Api/` (the `Order`/`Payment` aggregates in `Domain/{Orders,Payments}/`, DbContext in `Data/`, CQRS handlers in `Application/{Orders,Payments}/{Commands,Queries}`, controllers in `Controllers/`); public DTOs/requests/integration events in `src/Orders.Contracts/`. Deep-dive doc: [../architecture/modules/Orders.md](../architecture/modules/Orders.md).
- Host wiring/startup (`Program.cs`, `appsettings*.json`): `src/StarterKit.WebApi/`.
- Tests: `tests/Framework.Tests/<ProjectName>/...` (mirrors `Shared/`, `Infrastructure/`, `Persistence/`) and `tests/<Module>.Tests/<Area>/...` for each module (mirrors that module's own `<Module>.Api` folder structure, plus a `TestSupport/` folder for shared test infrastructure). No test project yet for `Notifications`.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-16_
