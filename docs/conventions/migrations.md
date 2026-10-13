# dotnet-ef

### Update Tools
```bash
dotnet tool install --global dotnet-ef
```

The migration commands below run from `src/Migrations/<Provider>` and use the global tool. The local tool manifest `src/StarterKit.WebApi/.config/dotnet-tools.json` (pinning `dotnet-ef`) applies only to commands run under `src/StarterKit.WebApi`.

### Get Context list
```
dotnet ef dbcontext list
```

### Add migrations
```
dotnet ef migrations add CreateIdentitySchema --context IdentityDbContext --output-dir Identity
dotnet ef migrations add CreateNotificationSchema --context NotificationDbContext --output-dir Notifications
```

Run from `src/Migrations/{Sqlite,PostgreSQL,MSSQL}` — pick the provider directory matching your target database; each module gets its own `--output-dir` under it. `--context` names the module's context (`IdentityDbContext`, `NotificationDbContext`) and selects which module's schema to migrate; the provider project must reference the module and register its context.

### Check for pending model changes
```
dotnet ef migrations has-pending-model-changes --context IdentityDbContext
```

### Update database
```
dotnet ef database update --context IdentityDbContext
```

Alternatively `dotnet run` in the provider directory migrates and seeds — see § Migration sets.

## Migration workflow

- **During development**, each schema change gets one incremental migration, added to `src/Migrations/MSSQL` only (named after the change, e.g. `AddUserCreatedIndex`). The other providers are not updated per change.
- **A full from-scratch regenerate** — deleting a module's migration chain and generating a single baseline named `Create<Module>Schema` (e.g. `CreateIdentitySchema`), for MSSQL and/or the other providers — happens only on the user's explicit command, once the module is complete. Until then a provider's baseline may lag the MSSQL chain, or be missing; the current per-provider state is in § Migration sets.

## Migration sets

Each provider directory under `src/Migrations/` is a console app holding one folder per module it carries. The provider is fixed by the project (`MSSQL`, `PostgreSQL`, `Sqlite`), not by `DbProvider`; the connection string is `ConnectionStrings:DefaultConnection` from the project's own `appsettings.json` (plus `appsettings.<ASPNETCORE_ENVIRONMENT>.json`, default `Staging`, and environment variables). Each module's `<Module>ContextInitialiser` applies its migrations (`InitialiseAsync` → `MigrateDatabaseAsync`) and each provider's `Program.cs` calls the initialisers of the modules it carries; modules with reference data also call `TrySeedAsync` (`Identity` does; `Notifications` seeds nothing). Seed rows are written at runtime by the initialisers, never by migrations. The host does not migrate at startup. The migrators ignore EF Core's pending-model-changes warning, so they apply a provider's migrations even when its snapshot lags the model.

| Modules | MSSQL | PostgreSQL | Sqlite |
|---|---|---|---|
| `Identity` | one `CreateIdentitySchema` baseline | one `CreateIdentitySchema` baseline | one `CreateIdentitySchema` baseline |
| `Notifications` | one `CreateNotificationSchema` baseline | one `CreateNotificationSchema` baseline | one `CreateNotificationSchema` baseline |

A baseline is generated from the module's current model, so its model snapshot is the source of truth for that module and provider.

All three migrators reference both modules and register both contexts, so a host on any provider gets both modules' tables from its migrator.

`dotnet ef migrations has-pending-model-changes` reports no pending changes for `Identity` and `Notifications` on every provider.

## Provider-aware filtered indexes

A filtered index has provider-specific predicate text (bracket-quoted identifiers and numeric booleans on SQL Server, double-quoted identifiers and real boolean literals on Npgsql). Declare it with `HasProviderFilter` (`src/Persistence/Extensions/IndexBuilderExtensions.cs`) instead of `HasFilter`: it applies the bracket form for every provider except Npgsql, which gets the quoted form. The `DbContext` passes `Database` and both texts:

```csharp
entity.HasIndex(x => x.IsBase)
    .IsUnique()
    .HasProviderFilter(
        Database,
        "[IsBase] = 1",
        "\"IsBase\" = true");
```

## Resetting a developer database

A database migrated on a different migration chain than the one now in the repository has different migration ids in `__EFMigrationsHistory` and cannot be updated in place. Drop and recreate it, or reset its `__EFMigrationsHistory` rows manually, before running the current baselines. This applies to an MSSQL database whose `Identity` history holds any migration other than `20260908114120_CreateIdentitySchema`, to a PostgreSQL database whose history holds any migration other than `20260930163058_CreateIdentitySchema` and `20260930163129_CreateNotificationSchema`, and to a Sqlite database whose history holds any migration other than `20261013064849_CreateIdentitySchema` and `20261013064902_CreateNotificationSchema`.

`__EFMigrationsHistory` is not placed in a module's schema: every module context on a database shares one history table in the connection's default schema (`public` on PostgreSQL). When a history table is reset, the module schemas (`identity`, `system`; on Sqlite, the module tables) must be dropped with it — otherwise the baselines fail on tables that already exist.

---
_Last synced: 2026-10-13_
