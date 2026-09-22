# dotnet-ef

### Update Tools
```bash
dotnet tool install --global dotnet-ef
```

### Add migrations
```
dotnet ef migrations add CreateIdentitySchema --context IdentityDbContext --output-dir Identity
```
```
dotnet ef migrations add CreateNotificationSchema --context NotificationDbContext --output-dir Notifications
```

Run from `src/Migrations/{Sqlite,PostgreSQL,MSSQL}` — pick the provider directory matching your target database; each module gets its own `--output-dir` under it. `--context <ModuleName>DbContext` selects which module's schema to migrate.

### Update database
```
dotnet ef database update --context NotificationDbContext
```

## Migration sets

Each provider directory under `src/Migrations/` holds one folder per module. Each module's `<Module>ContextInitialiser` applies its migrations (`MigrateDatabaseAsync`) and each provider's `Program.cs` calls the initialisers; modules with reference data also call `TrySeedAsync`. Seed rows are written at runtime by the initialisers, never by migrations.

| Modules | MSSQL | PostgreSQL | Sqlite |
|---|---|---|---|
| `Location`, `Catalog`, `Orders`, `Inventory`, `Transfers`, `Purchasing`, `Currency` | one `Create<Module>Schema` baseline | one `Create<Module>Schema` baseline | one `Create<Module>Schema` baseline |
| `Organization` | one baseline | one baseline | one baseline |
| `LeaveManagement` | baseline plus `AddLeaveRequestUserIdStatusIndex` and `ConvertLeaveRequestPeriodToOwnedType` | one baseline | one baseline |
| `Approval` | baseline plus `ApprovalStepLevelUniqueIndex` | one baseline (older than the MSSQL one — no concurrency-token column) | one baseline (same) |
| `Identity` | one baseline | baseline plus `AddUserCreatedIndex` | baseline plus `AddUserCreatedIndex` |
| `Notifications` | one baseline | none | none |

A baseline is generated from the module's current model, so its model snapshot is the source of truth for that module and provider.

## Provider-aware filtered indexes

A filtered index has provider-specific predicate text (bracket-quoted identifiers and numeric booleans on SQL Server, double-quoted identifiers and real boolean literals on Npgsql). Declare it with `HasProviderFilter` (`src/Persistence/Extensions/IndexBuilderExtensions.cs`) instead of `HasFilter`: it applies the bracket form for every provider except Npgsql, which gets the quoted form. Every module's `DbContext` passes both texts and `Database`:

```csharp
entity.HasIndex(x => x.IsBase)
    .IsUnique()
    .HasProviderFilter(
        Database,
        "[IsBase] = 1",
        "\"IsBase\" = true");
```

## Resetting a developer database

A database migrated on a different migration chain than the one now in the repository has different migration ids in `__EFMigrationsHistory` and cannot be updated in place. Drop and recreate it, or reset its `__EFMigrationsHistory` rows manually, before running the current baselines. This applies to a database created from the earlier incremental MSSQL-only chain of `Location`, `Catalog`, `Orders`, `Inventory`, `Transfers`, and `Purchasing`.
