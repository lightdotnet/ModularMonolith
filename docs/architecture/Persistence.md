# Project Overview: Persistence

## Purpose

`src/Persistence` (assembly/namespace `StarterKit.Persistence`) holds the framework's **EF Core building blocks**. Each module owns its own `DbContext`; `Persistence` gives that context what it shares with every other module:

- provider selection and `DbContext` registration from configuration, for InMemory, PostgreSQL, SQL Server (`MSSQL`), and Sqlite;
- an optional context base (`BaseDbContext`) and model-building helpers that keep a model valid on every provider;
- the save-path building blocks — audit stamping and domain-event dispatch — that a context calls from its own `SaveChanges` overrides;
- query-to-`Result` and paging helpers, provider-agnostic constraint-violation checks, opt-in cache and dynamic-table repositories;
- the services a migrate-and-seed app needs to run a module's migrations.

It references only `Shared`.

## Public Surface

**Context and registration**:

| Member | Role |
|---|---|
| `DbContextExtensions.AddConfiguredDbContext<TContext>(IConfiguration, string connectionName)` | Registers `TContext` for the provider in `DbProvider`: InMemory needs no connection string; any other provider requires `ConnectionStrings:<connectionName>` and throws `InvalidOperationException` at registration when it is missing |
| `DbContextExtensions.GetDbProvider` | Reads `DbProvider` as the `DbProvider` enum (`InMemory`, `PostgreSQL`, `MSSQL`, `Sqlite`) |
| `DbConnectionNames` | Connection-string names: `Default` and one per module (`Identity`); each resolves to `DefaultConnection` |
| `Context.BaseDbContext` | Abstract context that seals `OnModelCreating`: it calls the virtual `ConfigureModel(ModelBuilder)`, then applies the Sqlite `DateTimeOffset` conversion. It adds nothing to the save path |

**Save-path building blocks** (`Extensions/`):

| Member | Role |
|---|---|
| `TrackingExtensions.AuditEntries(userId, auditTime, enableSoftDelete)` | Stamps `Created`/`CreatedBy` on added and `LastModified`/`LastModifiedBy` on modified entries implementing the vendor audit interfaces; optionally turns deletes of `ISoftDelete` entities into updates stamped with `Deleted`/`DeletedBy`; returns a deleted one-to-one owned value object (`OwnsOne`) to unchanged, leaving collection-owned value objects deleted |
| `DispatchDomainEventsExtensions.DispatchDomainEvents(IPublisher, DbContext)` | Collects the domain events of every tracked entity, clears them from the entities, then publishes each one through the mediator, one after another |

**Model-building helpers** (`Extensions/`):

| Member | Role |
|---|---|
| `EntityTypeBuilderExtensions.ConfigureAuditableEntity` | Caps the string key and the audit-user columns of a `Shared` `AuditableEntity` at 450 characters (only the audit-user columns for `AuditableEntity<TId>`) |
| `IndexBuilderExtensions.HasProviderFilter` | Filtered-index predicate with provider-specific quoting — see [migrations.md § Provider-aware filtered indexes](../conventions/migrations.md#provider-aware-filtered-indexes) |
| `SqliteDbContextExtensions.FixSqliteDateTimeOffset` | On Sqlite only, converts every `DateTimeOffset`/`DateTimeOffset?` property to Unix seconds (`long`) |

**Queries and errors** (`Extensions/`):

| Member | Role |
|---|---|
| `QueryableResultExtensions` | `ToPagedAsync`/`ToPagedResultAsync` (vendor `Paged<T>`/`PagedResult<T>`, from explicit numbers or any `IPage`; page size capped at `MaxPageSize` = 100), `ToListResultAsync`, and `First`/`Last`/`SingleResultAsync`, which return a `NotFound` `Result<T>` instead of throwing |
| `DbUpdateExceptionExtensions` | `IsUniqueConstraintViolation` and `IsForeignKeyConstraintViolation` for SQL Server, PostgreSQL, and Sqlite, so a handler can turn an expected constraint race into a `Result` |

**Repositories** (`Repositories/`), opt-in:

| Type | Role |
|---|---|
| `ICacheRepository<T>`, `ICacheRepository<TEntity, TContext>` | Vendor `IRepository<T>`/`ISaveChanges` plus `RefreshCacheAsync`/`RemoveCacheAsync` |
| `CacheRepositoryBase<T>`, `CacheRepository<TEntity, TContext>` | Whole-table cache over the vendor EF repository: reads come from `ICacheService` (loaded with no tracking, 35-minute lifetime) and the repository's own `SaveChanges`/`SaveChangesAsync` refresh the cache |
| `DependencyInjection.AddCachingServices` | Registers the open generic `ICacheRepository<,>` (scoped); the cache provider itself comes from `Infrastructure`'s `AddAppCache` |
| `DynamicTableRepository<T, TEntity, TContext>` | Stores an object as rows of a vendor `DynamicEntity` table (one row per property, keyed by object name) and maps it back |

**Migration support** (`MigrationSupport/`):

| Member | Role |
|---|---|
| `MigrationsExtensions.AddMigrationsServices(params Assembly[] moduleAssemblies)` | Registers `ICurrentUser` as the internal `MigratorCurrentUser` (user id `Migrator`, singleton) and the mediator over the `Persistence` assembly plus the module assemblies passed in |
| `MigrationsExtensions.MigrateDatabaseAsync<TContext>(ILogger)` | Applies pending migrations when the context has any; logs and rethrows a failure |

## Configuration

| Key | Read by | Meaning |
|---|---|---|
| `DbProvider` | `AddConfiguredDbContext`, `GetDbProvider` | `InMemory`, `PostgreSQL`, `MSSQL`, or `Sqlite`; an absent key reads as `InMemory` (the enum's default) |
| `ConnectionStrings:<connectionName>` | `AddConfiguredDbContext` | Required for every provider except InMemory; module contexts pass a `DbConnectionNames` constant (`DefaultConnection`) |

The migrators do not read `DbProvider`: each fixes its provider in its own registration — see [migrations.md § Migration sets](../conventions/migrations.md#migration-sets). Host defaults: [Host § Configuration](Host.md#configuration).

## Usage

A module context that uses the base and wires the save path itself:

```csharp
internal sealed class BillingDbContext(
    ICurrentUser currentUser,
    IDateTime clock,
    IPublisher mediator,
    DbContextOptions<BillingDbContext> options)
    : BaseDbContext(options)
{
    protected override void ConfigureModel(ModelBuilder builder)
    {
        builder.HasDefaultSchema("billing");
        builder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.AuditEntries(currentUser.UserId, clock.AuditTime);

        await mediator.DispatchDomainEvents(this);

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
```

Registered from the module's `AppModule`:

```csharp
services.AddConfiguredDbContext<BillingDbContext>(
    configuration,
    DbConnectionNames.Default);
```

## Design Notes

- **Save path is the context's job**: `BaseDbContext` only builds the model. A context gets audit and domain-event dispatch by calling `AuditEntries` and `DispatchDomainEvents` from its `SaveChanges` overrides, in that order, before committing. Domain events are therefore handled before the commit, sequentially, and — because the mediator runs notification handlers in-line — in the caller's DI scope, so a handler's changes to the same context are committed with the originating save. The solution-level save path, including integration events, is in [architecture.md § Persistence save path](architecture.md#persistence-save-path).
- **Contexts that cannot derive the base**: `IdentityDbContext` derives from the ASP.NET Core Identity context, so it does not use `BaseDbContext`; it calls `AuditEntries`, `DispatchDomainEvents`, and `FixSqliteDateTimeOffset` directly — see [Identity § Design Notes](Identity.md#design-notes). No context in this solution derives from `BaseDbContext`.
- **Multi-provider**: a change to persistence behaviour must hold on every provider ([CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions)). Every InMemory context shares one database name (`InMemoryDb`).
- **Sqlite `DateTimeOffset`**: Sqlite cannot order by `DateTimeOffset`, so the conversion stores Unix seconds. Stored values lose sub-second precision and their offset (they read back as UTC). The conversion is applied after the context's own model configuration and replaces any converter configured there.
- **Pending model changes**: `AddConfiguredDbContext` configures EF Core's `PendingModelChangesWarning` to be logged, so a runtime context whose model differs from its migrations' snapshot does not fail when it migrates. The migrators ignore the warning entirely in their own registration — see [migrations.md § Migration sets](../conventions/migrations.md#migration-sets).
- **Migrator identity**: `AddMigrationsServices` makes every audit stamp written by a migrator read `Migrator`. Its mediator registration covers the `Persistence` assembly plus the module assemblies each migrator passes (the migrators pass the Identity assembly), so a module's notification handlers run during migrate-and-seed.
- **Cache repository contract**: an entity cached through `CacheRepositoryBase` must be written only through the repository — a save on the underlying context leaves the cache stale, and nothing enforces this. Cached instances are detached snapshots. The repository needs a relational provider (its cache key reads the connection's database name, which the InMemory provider cannot supply).
- **Dynamic-table concurrency**: `DynamicTableRepository.Update` is last-writer-wins; no concurrency token is configured.

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Shared` | project | `AuditableEntity`, `ICurrentUser`/`CurrentUserBase`, claim types, mediator and `Result` abstractions |
| `Lightsoft.EntityFrameworkCore` | package | Vendor EF repository base, `DynamicEntity` and its mapper, paging helpers |
| `Lightsoft.Caching` | package | `ICacheService` for the cache repositories |
| `Microsoft.EntityFrameworkCore.InMemory`, `.Sqlite`, `.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL` | package | The four supported providers |

Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](dependency-graph.md).

## Depended On By

| Project | Why |
|---|---|
| `Identity` | `AddConfiguredDbContext`, the save-path extensions, `FixSqliteDateTimeOffset`, `MigrateDatabaseAsync`, paging — see [Identity](Identity.md) |
| `Host` | Project reference only; host code calls no `Persistence` member — the module contexts it composes register through `AddConfiguredDbContext` — see [Host](Host.md) |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | `AddMigrationsServices`, `DbConnectionNames` — see [migrations.md](../conventions/migrations.md) |
| `tests/Framework.Tests` | Unit tests (Sqlite in-memory contexts where behaviour needs a database) |

Its only solution reference is `Shared`, which keeps the framework's dependency direction intact.

## Notable Conventions

- In an `entity.ToTable(...)` block, `HasIndex` calls come right after `ToTable` ([CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions)); filtered indexes use `HasProviderFilter`.
- The repository registration follows the `static class DependencyInjection` convention; the context, extension, and migration helpers are static extension classes named after what they extend.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
