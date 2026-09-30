# Coding Conventions: Backend

The short-form framework rules (packages, errors, API responses, DDD, events, DI, formatting, EF Core, tests) live in [CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions); this document adds the detail behind them.

## Build & Tooling

- Target framework: `net10.0` across every project, set once in `Directory.Build.props`.
- Central package management via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=false`). Every test project (`tests/Framework.Tests`, `tests/Identity.Tests`) imports the shared `tests/ModuleTests.props`, which opts out and pins the test packages (`xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq`) in one place. The three design-time migration projects under `src/Migrations/` also opt out — see [Deviations](#deviations-from-norms-elsewhere-in-the-repo).
- `Directory.Build.props` sets `ImplicitUsings=enable` and `Nullable=enable` repo-wide.
- Module structure: flat projects directly under `src/` (no `src/Modules/` nesting), plus a `<Module>.Contracts` seam per module — see [CLAUDE.md § 1](../../CLAUDE.md#1-repository-purpose) and [../architecture/dependency-graph.md](../architecture/dependency-graph.md). One module exists: Identity (`src/Identity` — assembly `StarterKit.Modules.Identity` — plus `src/Identity.Contracts` and `src/Identity.Web`); its detail lives in [../architecture/Identity.md](../architecture/Identity.md).
- Vendor library family: `Lightsoft.*` (namespace `Light.*`) supplies the mediator, `Result`/`Paged` contracts, domain base types, ASP.NET Core authorization/modularity/CORS/Swagger helpers, EF Core helpers, caching, and Serilog setup — treat as fixed external API, not renameable/refactorable project code.

## Style

- No root `.editorconfig`.
- Nullable reference types enabled everywhere; file-scoped namespaces consistently.
- PascalCase for all constants and enum members (no `SCREAMING_SNAKE_CASE`).
- Folder names mirror the trailing namespace segment (e.g. `src/Identity/Features/Users/Commands/` ⇒ `StarterKit.Modules.Identity.Features.Users.Commands`).
- CQRS command/query types: `internal sealed record` (never `public` — the controller is in the same assembly and the type is not part of any seam), one file per feature named after the feature (`CreateUser.cs`, not `CreateUserCommand.cs`), holding the command/query, its validator (see § Validation below), and its handler. A command/query wraps the request DTO (`CreateUserCommand(CreateUserRequest Model)`) or takes primitives for trivial payloads (`DeleteUserCommand(string Id)`); the controller binds the DTO and constructs the command/query.

## Structural Conventions

- **Domain design is DDD-first** — model aggregates/invariants/value objects/domain events before handlers, rules on the aggregate not the handler/service. See [CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions). **The aggregate enforces only real domain invariants — not input-shape validation.** Required/length/enum-range checks on an incoming request are FluentValidation's job (§ Validation below), not the aggregate's factory/mutator methods.
- DI registration via small `static class DependencyInjection` extension classes exposing `Add<Feature>`/`Use<Feature>` methods, one per feature area/folder. The host is the exception — see [../architecture/Host.md § Notable Conventions](../architecture/Host.md#notable-conventions).
- Result pattern: vendor `Light.Contracts.Result`/`Result<T>`/`PagedResult<T>` for expected-failure outcomes; `Light.Exceptions.ValidationException` (thrown by `ValidationBehaviour<,>`) for request validation failures.
- **Validation — two-layer FluentValidation.** The `ValidationBehaviour<,>` pipeline behavior and the `AddValidatorsFromAssemblies` wiring (`src/Host/ConfigureExtensions.cs`, over the host's assembly scan list) are in place; no project registers a validator yet. The shape to follow: each request DTO gets an `AbstractValidator<TRequest>` **in the same file**, holding field-shape rules only (`NotEmpty`, `MaximumLength`, `IsInEnum`); each mediator command gets a thin `AbstractValidator<TCommand>` **in the same file as the command+handler**, validating route-level primitives directly (e.g. `RuleFor(x => x.Id).NotEmpty()`) and delegating the DTO via `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. Queries generally don't need a validator.
- Mediator pipeline behaviors (registered in `src/Host/ConfigureExtensions.cs`, outermost first): `LoggingBehaviour<,>` (`src/Shared` — logs request type name + elapsed time only, never bodies), then `ValidationBehaviour<,>` (FluentValidation).
- Logging: `AppLogging` (`src/Infrastructure`) static Serilog logger for bootstrap/startup; standard `ILogger<T>` DI for request/runtime logging elsewhere.
- **CQRS handler shape**: controller write actions dispatch an `internal` mediator command/query. In `Identity`, the command handlers forward to a service class (`IUserService`, `IRoleService`), `SearchUserQueryHandler` queries `UserManager<User>` directly, and several read actions call those services directly rather than through a query — see [Deviations](#deviations-from-norms-elsewhere-in-the-repo).
- **Audience-split controllers**: an admin controller and a self-service controller hard-scoped via `ICurrentUser` over the same data — `Identity`'s `UserController` and `UserProfileController`.
- Every module gets a `<Module>.Contracts` seam. `Shared` is the solution's only true leaf — `Identity.Contracts` references `Shared`, so "seam project" does not imply "leaf project".
- **A cross-module seam (`I<Module>…Api`/`I<Module>…Service`) is a DI-only interface on the module's `Contracts` project, implemented `internal` in the module**, for another module to call directly via constructor injection (not `Mediator.Send`, which stays `internal` to the owning module). `Identity`'s `IIdentityModuleApi` (implemented in `src/Identity/Api/`) is the reference — see [../architecture/Identity.md](../architecture/Identity.md).
- **A filtered index is declared with `HasProviderFilter`** (`src/Persistence/Extensions/IndexBuilderExtensions.cs`), not the vendor `HasFilter` directly, so the predicate text is right on every configured provider — see [migrations.md § Provider-aware filtered indexes](migrations.md#provider-aware-filtered-indexes).

## Testing Conventions

- Framework: xUnit v3 (`xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) on Microsoft.Testing.Platform. On the .NET 10 SDK `dotnet test` fails, so tests run through the built test executable — see [development-guide.md § Running Tests](development-guide.md#running-tests).
- Naming: `<TypeUnderTest>Tests` classes; `MethodOrMember_ShouldExpectedBehavior_WhenCondition` methods; `// Arrange`/`// Act`/`// Assert` comments.
- `Framework.Tests` mostly uses hand-written fakes/test doubles (`RecordingPublisher : IPublisher`, `TestCurrentUser : CurrentUserBase`, `FakeCurrentUser : ICurrentUser`), with `Moq` where a logger or similar dependency needs a double, and a Sqlite in-memory `DbContext` for persistence behavior. `Identity.Tests` uses `Moq` for the module's services and framework dependencies (`UserManager<User>`, `IMediator`, `IEventBus`, `IUserService`, …) and runs persistence-dependent tests against a real Sqlite in-memory `DbContext` via `IdentityTestHost` (`TestSupport/`).
- **Mocking an `internal` interface with Moq needs a second `InternalsVisibleTo` grant, to `"DynamicProxyGenAssembly2"`.** The usual `InternalsVisibleTo("<Module>.Tests")` is enough to let the test project *reference* an internal type, but Moq's Castle DynamicProxy builds its proxy in a dynamically generated assembly named `DynamicProxyGenAssembly2`, which separately needs visibility to implement an `internal interface`. No project needs it today (the interfaces `Identity.Tests` mocks are `public`); a project hitting the issue adds `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` rather than making the interface `public`.
- Test project layout mirrors the source project's folder structure — `tests/Framework.Tests/<ProjectName>/...` for the framework projects and `tests/<Module>.Tests/<Area>/...` for a module — plus a `TestSupport/` folder. `InternalsVisibleTo` is set on `Identity.csproj` (for `Identity.Tests`) and on `Shared`/`Infrastructure`/`Persistence` (for `Framework.Tests`) so tests reach `internal` types.
- **Coverage**: `tests/Framework.Tests` covers the framework projects; each module has one `tests/<Module>.Tests` project (`tests/Identity.Tests`).

## Deviations From Norms Elsewhere in the Repo

- **`Identity`'s handlers delegate to service classes.** Its command handlers forward to `IUserService`/`IRoleService` instead of holding the logic (the search query uses `UserManager<User>` directly), and its read endpoints and token endpoints call services directly. New modules put behavior on the aggregate and logic in the handler.
- **Migration projects version their own packages.** `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` set `ManagePackageVersionsCentrally=false` and put `Version="$(AspnetVersion)"` on their EF Core/hosting `PackageReference`s, unlike every other `src/` project — see [migrations.md](migrations.md).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
