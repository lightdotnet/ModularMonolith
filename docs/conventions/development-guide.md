# Development Guide: Backend

## Prerequisites

- A .NET SDK that targets `net10.0`.
- A reachable database matching the configured `DbProvider` (see Running Locally below) if running the host against a real database — no external service is required just to run the test suite.
- The `dotnet-ef` tool for migration work — see [migrations.md § Update Tools](migrations.md#update-tools).

## Building

```
dotnet build StarterKit.slnx
```

## Running Locally

```
dotnet run --project src/Host/Host.csproj
```

The default `http` launch profile binds plain HTTP on `http://localhost:5000`, which is the primary Development endpoint — HTTPS redirection is skipped in `Development`, so no HTTPS listener is needed for local work (this also lets a server-to-server client call the API without hitting the untrusted dev certificate). The `https` profile additionally binds `https://localhost:5001` (ASP.NET dev certificate) for anyone who wants it.

`src/Host` is the composition root — see [../architecture/Host.md](../architecture/Host.md). Its local defaults:

- **Database**: `DbProvider` is `MSSQL` in `src/Host/appsettings.json`, pointing `ConnectionStrings:DefaultConnection` at a local `(localdb)\mssqllocaldb` instance, and `appsettings.Development.json` does not override it. A relational database needs its schema created first by the matching migrator — see [migrations.md § Migration sets](migrations.md#migration-sets). Switch to `InMemory` (no schema step, no database server) or `Sqlite`/`PostgreSQL` via `DbProvider` and a matching `ConnectionStrings:DefaultConnection` (commented-out PostgreSQL/Sqlite examples are in `appsettings.json`) if SQL Server LocalDB is not available.
- **Event bus**: `RabbitMQ:Enable` is `false`, so no broker is needed.
- **Authentication**: `AllowAnonymous` is `false` in `appsettings.json` and not overridden in Development — requests need a valid JWT unless the endpoint is explicitly anonymous.

An existing developer database that no longer matches the current migration ids must be reset first — see [migrations.md § Resetting a developer database](migrations.md#resetting-a-developer-database).

## Running Tests

The root `global.json` selects the Microsoft.Testing.Platform runner for `dotnet test` (required on the .NET 10 SDK):

```
dotnet test --solution StarterKit.slnx
dotnet test --project tests/Identity.Tests/Identity.Tests.csproj
```

Narrow a run by passing runner options after `--`, e.g. `-- --filter-class <FullyQualifiedClassName>` or `-- --filter-method <FullyQualifiedMethodName>`; a built test project's executable (`tests/<Project>/bin/Debug/net10.0/<Project>.exe`) accepts the same options directly.

No external database or service is needed. Test conventions and coverage: [coding-conventions.md § Testing Conventions](coding-conventions.md#testing-conventions).

## Local Setup

- The checked-in connection string, JWT signing values (`Jwt:SecretKey`, `Jwt:Issuer`, token lifetimes), and Basic Auth value (`BasicAuth`) in `src/Host/appsettings.json` are starter-template placeholders, not production secrets — replace them before any real deployment.
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
| Add a migration | [migrations.md § Add migrations](migrations.md#add-migrations) and [§ Migration workflow](migrations.md#migration-workflow) (which providers to update when) |
| Create/update a database schema and seed it | `dotnet run` from `src/Migrations/<Provider>` — see [migrations.md § Migration sets](migrations.md#migration-sets) |
| Run local infra (Postgres, Redis, pgAdmin) via Docker | [docker-cli.md](docker-cli.md) |

## Where to Look for X

Which project owns what: [../architecture/architecture.md § Layering](../architecture/architecture.md#layering), with a linked overview per project. Test layout: [coding-conventions.md § Testing Conventions](coding-conventions.md#testing-conventions).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
