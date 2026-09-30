# Development Guide: Backend

## Prerequisites

- A .NET SDK that targets `net10.0`.
- A reachable database matching the configured `DbProvider` (see Local Setup below) if running the host against a real database — no external service is required just to run the test suite.
- The `dotnet-ef` tool for migration work — see [migrations.md § Update Tools](migrations.md#update-tools).

## Building

```
dotnet build StarterKit.slnx
```

## Running Locally

```
dotnet run --project src/Host/Host.csproj
```

The default `http` launch profile binds plain HTTP on `http://localhost:5000`, which is the primary Development endpoint — `Program.cs` skips `UseHttpsRedirection()` when `IsDevelopment()`, so no HTTPS listener is needed for local work (this also lets a server-to-server client call the API without hitting the untrusted dev certificate). The `https` profile additionally binds `https://localhost:5001` (ASP.NET dev certificate) for anyone who wants it; outside Development the HTTPS redirect is active as normal.

`src/Host` is the composition root; what it registers and the order of its pipeline are described in [../architecture/Host.md](../architecture/Host.md). By default (`src/Host/appsettings.json`) `DbProvider` is `MSSQL`, pointing `ConnectionStrings:DefaultConnection` at a local `(localdb)\mssqllocaldb` instance, and `appsettings.Development.json` does not override it. The host does not migrate at startup, so a relational database needs its schema created first by the matching migrator — see [migrations.md § Migration sets](migrations.md#migration-sets). Switch to `InMemory` (no schema step, no database server) or `Sqlite`/`PostgreSQL` via `IConfiguration["DbProvider"]` (and a matching `ConnectionStrings:DefaultConnection`; commented-out examples for PostgreSQL/Sqlite are already present in `appsettings.json`) if you don't have SQL Server LocalDB available. `RabbitMQ:Enable` is `false`, so the no-op event bus is registered and no broker is needed. `AllowAnonymous` is `false` by default in `appsettings.json` (`appsettings.Development.json` does not override it) — requests need a valid JWT unless the endpoint is explicitly anonymous.

An existing developer database that no longer matches the current migration ids must be reset first — see [migrations.md § Resetting a developer database](migrations.md#resetting-a-developer-database).

## Running Tests

On the .NET 10 SDK `dotnet test` fails ("Testing with VSTest target is no longer supported by Microsoft.Testing.Platform"). Build, then run each test project's executable directly:

```
dotnet build StarterKit.slnx
tests/Framework.Tests/bin/Debug/net10.0/Framework.Tests.exe
tests/Identity.Tests/bin/Debug/net10.0/Identity.Tests.exe
```

Narrow a run with `-class <FullyQualifiedClassName>` or `-method <FullyQualifiedMethodName>`.

No special setup needed — all current tests are unit tests with mocked dependencies or EF Core's Sqlite in-memory provider (no external database/services required). `Framework.Tests` covers `Shared`, `Infrastructure`, `Persistence`, and `EventBusMassTransitRabbitMQ`; `Identity.Tests` covers `Identity`. Test conventions: [coding-conventions.md § Testing Conventions](coding-conventions.md#testing-conventions).

## Local Setup

- Default `DbProvider` is `MSSQL` (`src/Host/appsettings.json`) with a `(localdb)\mssqllocaldb` connection string checked into `appsettings.json` as a starter-template default — replace for real use, do not treat as a production secret.
- JWT signing (`Jwt:SecretKey`, `Jwt:Issuer`, token lifetimes) and Basic Auth (`BasicAuth: "super:123"`) values in `appsettings.json` are template placeholders, not production secrets — replace before any real deployment.
- `UserSecretsId` is set on `src/Host/Host.csproj` for local `dotnet user-secrets` overrides if preferred over editing `appsettings.Development.json` directly — e.g. the Microsoft Entra ID client secret:

  ```
  dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<SECRET_VALUE>" --project src/Host/Host.csproj
  dotnet user-secrets list --project src/Host/Host.csproj
  dotnet user-secrets remove "Authentication:Microsoft:ClientSecret" --project src/Host/Host.csproj
  ```

- Configuration sections and their owners: [../architecture/Host.md § Configuration](../architecture/Host.md#configuration).

## Common Tasks

| Task | How |
|---|---|
| Add a migration | `dotnet ef migrations add <Name> --context IdentityDbContext --output-dir Identity`, run from `src/Migrations/<Provider>` where `<Provider>` is `MSSQL`, `PostgreSQL`, or `Sqlite`. See [migrations.md](migrations.md) for the migration workflow (which providers to update when), the current migration set per provider, and the EF CLI cheat sheet. |
| Create/update a database schema and seed it | `dotnet run` from `src/Migrations/<Provider>` — see [migrations.md § Migration sets](migrations.md#migration-sets). |
| Run local infra (Postgres, Redis, pgAdmin) via Docker | See [docker-cli.md](docker-cli.md). |
| Run the API locally | `dotnet run --project src/Host/Host.csproj` |
| Run the test suite | Build, then run each test executable — see § Running Tests. |
| Build the whole solution | `dotnet build StarterKit.slnx` |

## Where to Look for X

- Shared abstractions/base types (`ICurrentUser`, `IDateTime`, `Status`, `BaseDto`, entity wrappers, `IntegrationEvent`, authorization, mediator pipeline behaviors): `src/Shared/`.
- Cross-cutting infrastructure (CORS, health checks, caching, Serilog bootstrap logging, Mapster config, controller bases, module/endpoint base classes): `src/Infrastructure/`.
- EF Core provider config, DbContext base class, audit/soft-delete tracking, domain-event dispatch, migration support, the provider-aware filtered-index helper: `src/Persistence/`.
- Integration-event bus over MassTransit/RabbitMQ, consumer bases: `src/EventBusMassTransitRabbitMQ/` — see [../architecture/EventBusMassTransitRabbitMQ.md](../architecture/EventBusMassTransitRabbitMQ.md).
- Identity module (users, roles, claims, sessions, JWT auth, external login): `src/Identity/` (entities in `Domain/`, `IdentityDbContext` and its initialiser in `Persistence/`, CQRS handlers in `Features/<Area>/{Commands,Queries}`, services in `Services/` + `Authentication/{Jwt,ExternalLogin}`, controllers in `Endpoints/`, request/response DTOs in `Models/`, the `IIdentityModuleApi` implementation in `Api/`); the cross-module seam (`IIdentityModuleApi`, `UserSummary`, integration events) in `src/Identity.Contracts/`; Razor Pages login and the external-login relay in `src/Identity.Web/`. Deep-dive doc: [../architecture/Identity.md](../architecture/Identity.md).
- Host wiring/startup (`Program.cs`, `ConfigureExtensions.cs`, `appsettings*.json`): `src/Host/` — see [../architecture/Host.md](../architecture/Host.md).
- Design-time EF migrations and the migrate-and-seed console apps: `src/Migrations/{MSSQL,PostgreSQL,Sqlite}/` — see [migrations.md](migrations.md).
- Tests: `tests/Framework.Tests/<ProjectName>/...` (mirrors `Shared/`, `Infrastructure/`, `Persistence/`, `EventBusMassTransitRabbitMQ/`) and `tests/Identity.Tests/<Area>/...` (mirrors `src/Identity`'s folder structure, plus a `TestSupport/` folder for shared test infrastructure).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
