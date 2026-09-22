# Module Overview: Currency

## Purpose

Owns which currencies the system can hold amounts in and the rates that convert them to the **base
currency**, via two entities sharing the module's one `CurrencyDbContext`.

- **`Currency`** — identified by its ISO 4217 code (a caller-supplied, upper-cased, three-letter string used
  as the key, the same shape as Location's `LocationType`). It carries a name, an optional symbol,
  `DecimalPlaces` (0..4, the minor units amounts in this currency are rounded to), and `IsActive`. **Exactly
  one currency is the base** (`IsBase`): it is created only through `Currency.CreateBase` by the seeder,
  every currency created through the API goes through `Currency.Create` and is never the base, and no
  operation flips the flag. The base can never be deactivated and its decimal places can never change.
  Decimal places of any currency also cannot change once a rate has been recorded for it. Changing which
  currency is the base is out of scope: there is no endpoint for it.
- **`ExchangeRate`** — one recorded rate, "1 unit of the currency = `Rate` units of the base currency",
  effective from `EffectiveFrom` until a newer rate takes over. The history is **append-only**: there is no
  edit or delete, and a correction is a newer rate. A rate must be positive, cannot be recorded for the base
  currency or for an inactive currency, and must be **strictly later** than the newest existing rate of the
  same currency.

Other modules consume the module through `ICurrencyService` (see Notable Conventions); it has no other
cross-module surface and publishes no events.

## Internal Layering

Currency is a **single-project module** (not split Domain/Application/Infrastructure/Api), the same
structural convention as `Location`/`Catalog`/`Orders`/`Inventory`/`Transfers`/`Purchasing`:

| Project | Responsibility | Notes |
|---|---|---|
| `Currency.Contracts` | DTOs, requests, the cross-module seam, and the permission catalog, in per-feature subfolders (`Common/` — `CurrencyLimits`, the shared bounds used by validators and the EF mapping; `Currencies/`; `ExchangeRates/`; `Services/` — `ICurrencyService`, `CurrencyInfoDto`, `ExchangeRateQuote`, `ExchangeRateNotFoundException`, `CurrencyRounding`; `Authorization/`). Each request carries its own `AbstractValidator` in the same file. Declares `Lightsoft.AspNetCore.Authorization` directly; references `Shared`. |
| `Currency.Api` | Single project organized by folder: `Domain/Currencies/` (the `Currency` aggregate — private ctor, `Create`/`CreateBase` factories, `Rename`/`SetSymbol`/`SetDecimalPlaces`/`Activate`/`Deactivate` — and `CurrencyByCodeSpec`), `Domain/ExchangeRates/` (the `ExchangeRate` entity with its `Record` factory, and the `EffectiveAt` query extension). `Data/` (`CurrencyDbContext`, `CurrencyContextInitialiser`). `Services/` (`CurrencyService`, `internal`, implementing `ICurrencyService`). `Application/{Currencies,ExchangeRates}/{Commands,Queries}` — handlers own their `CurrencyDbContext` logic directly, plus a thin per-command `AbstractValidator`. `Controllers/` (`CurrencyController`, `ExchangeRateController`). `CurrencyModule.cs` (DI: DbContext, `ICurrencyService`, permission provider). |

`Currency.Api.csproj` sets a plural `RootNamespace`/`AssemblyName` (`StarterKit.Currencies.*`) while the
project and folder names stay singular — the same namespace-collision avoidance `Location` uses, since a type
named `Currency` would otherwise collide with a namespace segment.

## Public Contract

`CurrencyController` (route `currency`, class-level `currency.currencies.view`; the id in a route is the ISO
code):

| Route | Verb | Permission | Notes |
|---|---|---|---|
| `currency` | GET | `currencies.view` | `SearchCurrencyRequest` (`SearchValue` matches code or name, `IsActive?`, paging) |
| `currency/{code}` | GET | `currencies.view` | One currency |
| `currency` | POST | `currencies.manage` | `CreateCurrencyRequest { Code, Name, Symbol?, DecimalPlaces }`; a code must be three letters, `DecimalPlaces` 0..4 |
| `currency/{code}` | PUT | `currencies.manage` | `UpdateCurrencyRequest` — name, symbol, decimal places (subject to the domain rules above) |
| `currency/{code}/activate` | PUT | `currencies.manage` | |
| `currency/{code}/deactivate` | PUT | `currencies.manage` | Refused for the base currency |

`ExchangeRateController` (route `exchange_rate`, class-level `currency.rates.view`):

| Route | Verb | Permission | Notes |
|---|---|---|---|
| `exchange_rate` | GET | `rates.view` | `SearchExchangeRateRequest` (`CurrencyCode?`, `From?`/`To?` bounding `EffectiveFrom`, both inclusive, paging) |
| `exchange_rate/latest` | GET | `rates.view` | The rate in effect per **active foreign** currency, as of now or the optional `asOf`; a currency with no rate effective yet is omitted |
| `exchange_rate` | POST | `rates.manage` | `RecordExchangeRateRequest { CurrencyCode, Rate, EffectiveFrom, Note? }`; the recorder is the caller |

`CurrencyPermissions`: `Currencies.{View,Manage}` and `Rates.{View,Manage}` (group `currency`) — the
per-feature `View`/`Manage` simplification `Location`/`Catalog`/`Orders` use. `Currencies.Manage` also covers
activate/deactivate; `Rates.Manage` records a rate (there is no other rate write).

## Data Access

`CurrencyDbContext : BaseDbContext`, schema `"currency"`, registered via
`AddConfiguredDbContext<CurrencyDbContext>(configuration, DbConnectionNames.Currency)`;
`DbConnectionNames.Currency` aliases `Default` — the same physical database as every other module, separated
by schema + table name.

Two tables:

- **`Currencies`** — string key = the ISO code (`CurrencyLimits.CodeLength`, 3); a **filtered unique index
  on `IsBase`** (where true) is the database backstop for "at most one base currency", written with
  `HasProviderFilter` so the filter text is right per provider (see
  [../architecture.md](../architecture.md#key-design-patterns)). Configured with the parameterless
  `ConfigureAuditableEntity()`.
- **`ExchangeRates`** — `bigint IDENTITY(1,1)` key (`AuditableEntity<long>`); unique index on
  `(CurrencyCode, EffectiveFrom)`, which also serves the "rate effective at time T" lookup; `Rate` is
  `decimal(18,8)`; `RecordedBy` max length 450; FK to `Currencies` with `Restrict`. The recorded rate is
  bounded by `CurrencyLimits.MaxRate` (1e9, below what the column holds) and at most 8 decimal places.

**Append-only backstop:** `CurrencyDbContext.SaveChanges[Async]` calls `EnsureExchangeRatesAreAppendOnly`
before auditing and throws a `ConflictException` if any tracked `ExchangeRate` is `Modified` or `Deleted`, so
even code that bypasses the domain cannot rewrite history. No entity implements `ISoftDelete` and the context
dispatches no domain events.

`CurrencyContextInitialiser.InitialiseAsync()` applies migrations; `TrySeedAsync()` (called by each provider's
migrator `Program.cs`) idempotently seeds the base currency **VND** (`CurrencyConstants.Default`, no minor
units) only when no base currency exists yet — an existing base is never touched, whichever currency it is —
and throws if a non-base `VND` exists while no base is configured. A concurrent seeder that loses the insert
race succeeds silently when a base now exists.

Migrations: see [../../conventions/migrations.md](../../conventions/migrations.md).

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Currency.Contracts → Shared`) | `AuditableEntity`/`AuditableEntity<long>`, `PageQuery`/`SearchQuery`, `ICurrentUser`/`IDateTime`, `CurrencyConstants`. |
| `Infrastructure` | project (`Currency.Api → Infrastructure`) | `VersionedApiController`, `AppModule`. |
| `Persistence` | project (`Currency.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, audit tracking, `HasProviderFilter`, paging, `IsUniqueConstraintViolation`, migration support. |
| `Currency.Contracts` | project (`Currency.Api → Currency.Contracts`) | The module's own seam. |
| Vendor `Lightsoft.AspNetCore.Authorization` (both projects), `Lightsoft.EntityFrameworkCore`, `Lightsoft.Mediator`, `Lightsoft.Result` | package, all declared directly | No undeclared-transitive-dependency instance. |

`Currency` has **no outgoing dependency on any other business module**.

## Depended On By

- `StarterKit.WebApi` — composition-root host (`ConfigureExtensions.cs`'s `assemblies` array).
- `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` — each references `Currency.Api` for the `DbContext`/initialiser.
- `Currency.Tests` — `Currency.Api.csproj` grants `InternalsVisibleTo` for the test project plus
  `DynamicProxyGenAssembly2` for Moq.
- **`Orders.Api`** — `ICurrencyService` (`GetBaseCurrencyAsync`, `GetRateToBaseAsync`) and `CurrencyRounding`,
  to create orders in the base currency and convert foreign-priced products when a line is added (see
  [Orders.md](Orders.md)).
- **`Catalog.Api`** — `ICurrencyService.GetAsync`, to check that a product's price currency is an active
  currency (see [Catalog.md](Catalog.md)).

`Transfers`, `Inventory`, and `Purchasing` do not reference `Currency` (see [../../known-debt.md](../../known-debt.md)
for `Purchasing`'s fixed purchase-cost currency).

## Notable Conventions

- **`ICurrencyService` is the module's read-only, DI-only cross-module seam** (no HTTP surface). It offers
  `GetBaseCurrencyAsync` (throws `InvalidOperationException` when no base is configured), `GetAsync` (by
  case-insensitive code; `null` for an unknown or blank code), and `GetRateToBaseAsync` /
  `GetRatesToBaseAsync` (the batch form: one quote per distinct code, input order kept). A quote carries the
  currency, the base currency code, the rate, and the `EffectiveFrom` of the rate used.
- **Rate resolution:** the rate with the greatest `EffectiveFrom` **not after** the requested instant
  (inclusive). The base currency resolves to rate 1 with no `EffectiveFrom`. An unknown currency, an inactive
  currency, or one with no rate effective yet throws `ExchangeRateNotFoundException` — a
  `Light.Exceptions.ValidationException`, so callers surface it as a 4xx. **A missing rate is never a silent
  1:1.** `CurrencyService` is scoped and holds no cache; each call queries the database.
- **`CurrencyRounding.RoundToMinorUnits`** rounds midpoints away from zero (0.5 becomes 1, -0.5 becomes -1),
  so every module rounds converted amounts the same way.
- **Append-only history.** Corrections are new rates dated later than the newest one; a rate cannot be
  backdated. `RecordExchangeRateCommand` reads the newest `EffectiveFrom` and passes it to
  `ExchangeRate.Record`, which owns the ordering rule; the unique index only catches a concurrent insert of
  the same instant, which the handler maps to the same conflict. A rate may be dated at most one day ahead
  of now (`CurrencyLimits.MaxFutureEffectiveFrom`), checked in the command validator because it needs the
  clock; a future-dated rate is not returned by `latest` until it takes effect.
- **Domain rules versus input rules.** The aggregates enforce only real invariants (base rules, rate
  ordering, positive rate, decimal-place lock); code shape, length, the 0..4 decimal range, the rate bound,
  and the rate scale are FluentValidation's job on the request/command, the same two-layer split every
  module uses.
- **The base currency is data, not a rule.** `CurrencyConstants.Default` (`"VND"`) in `Shared` is only the
  code the seeder uses when no base exists; nothing else may assume it (see
  [../../known-debt.md](../../known-debt.md)).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-21_
