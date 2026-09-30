# Coding Conventions: Backend

The short-form framework rules (packages, errors, API responses, DDD, events, DI, formatting, EF Core, tests) live in [CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions); this document adds the detail behind them. Project layout and dependency rules: [CLAUDE.md § 1](../../CLAUDE.md#1-repository-purpose).

## Build & Tooling

- `Directory.Build.props` sets `net10.0`, `ImplicitUsings=enable`, and `Nullable=enable` for every project.
- Central package management via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=false`). The projects that opt out — the test projects through `tests/ModuleTests.props`, and the migration projects — are listed in [dependency-graph.md § Version Mismatches](../architecture/dependency-graph.md#version-mismatches).
- Vendor `Lightsoft.*` (namespace `Light.*`) types are a fixed external API, not renameable/refactorable project code.

## Style

- No root `.editorconfig`.
- File-scoped namespaces consistently.
- PascalCase for all constants and enum members (no `SCREAMING_SNAKE_CASE`).
- Folder names mirror the trailing namespace segment (e.g. `src/Identity/Features/Users/Commands/` ⇒ `StarterKit.Modules.Identity.Features.Users.Commands`).
- CQRS command/query types: `internal sealed record` (never `public` — the controller is in the same assembly and the type is not part of any seam), one file per feature named after the feature (`CreateUser.cs`, not `CreateUserCommand.cs`), holding the command/query, its validator (see § Validation below), and its handler. A command/query wraps the request DTO (`CreateUserCommand(CreateUserRequest Model)`) or takes primitives for trivial payloads (`DeleteUserCommand(string Id)`); the controller binds the DTO and constructs the command/query.

## Structural Conventions

- **Domain design is DDD-first** — model aggregates, invariants, value objects, and domain events before handlers ([CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions)).
- **DI registration**: exceptions to the `DependencyInjection` naming convention are noted in each project's overview under Notable Conventions (e.g. the host composes through `ConfigureExtensions` — see [../architecture/Host.md § Notable Conventions](../architecture/Host.md#notable-conventions)).
- **Validation — two-layer FluentValidation.** The `ValidationBehaviour<,>` pipeline behavior and the host's validator registration are in place; no project defines a validator yet. The shape to follow: each request DTO gets an `AbstractValidator<TRequest>` **in the same file**, holding field-shape rules only (`NotEmpty`, `MaximumLength`, `IsInEnum`); each mediator command gets a thin `AbstractValidator<TCommand>` **in the same file as the command+handler**, validating route-level primitives directly (e.g. `RuleFor(x => x.Id).NotEmpty()`) and delegating the DTO via `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. Queries generally don't need a validator.
- **Logging**: `AppLogging` ([Infrastructure](../architecture/Infrastructure.md)) only for bootstrap/startup; standard `ILogger<T>` DI for request/runtime logging everywhere else.
- **Audience-split controllers**: an admin controller and a self-service controller hard-scoped via `ICurrentUser` over the same data — `Identity`'s `UserController` and `UserProfileController`.
- **Seam vs. leaf**: a `<Module>.Contracts` seam references `Shared`, so "seam project" does not imply "leaf project"; `Shared` is the only leaf.
- **A cross-module seam (`I<Module>…Api`/`I<Module>…Service`) is a DI-only interface on the module's `Contracts` project, implemented `internal` in the module**, for another module to call directly via constructor injection (not `Mediator.Send`, which stays `internal` to the owning module). `Identity`'s `IIdentityModuleApi` (implemented in `src/Identity/Api/`) is the reference — see [../architecture/Identity.md](../architecture/Identity.md).
- **Filtered indexes** are declared with `HasProviderFilter` — see [migrations.md § Provider-aware filtered indexes](migrations.md#provider-aware-filtered-indexes).

## Testing Conventions

- Framework: xUnit v3 + Moq, pinned by `tests/ModuleTests.props`; how to run them: [development-guide.md § Running Tests](development-guide.md#running-tests).
- Coverage: `tests/Framework.Tests` covers the framework projects; each module has one `tests/<Module>.Tests` project (`tests/Identity.Tests`).
- Layout: a test project mirrors its source projects' folder structure — `tests/Framework.Tests/<ProjectName>/...` for the framework projects and `tests/<Module>.Tests/<Area>/...` for a module; `tests/Identity.Tests` adds a `TestSupport/` folder for shared test infrastructure.
- Naming: `<TypeUnderTest>Tests` classes; `MethodOrMember_ShouldExpectedBehavior_WhenCondition` methods; `// Arrange`/`// Act`/`// Assert` comments.
- Doubles: `Framework.Tests` mostly uses hand-written private fakes (e.g. a recording `IPublisher`, a `CurrentUserBase` subclass), with `Moq` where a logger or similar dependency needs a double, and a Sqlite in-memory `DbContext` for persistence behavior. `Identity.Tests` uses `Moq` for the module's services and framework dependencies and runs persistence-dependent tests against a real Sqlite in-memory `DbContext` via `IdentityTestHost` (`TestSupport/`).
- Internal access: a source project grants `InternalsVisibleTo` to its test project so tests reach `internal` types.
- **Mocking an `internal` interface with Moq needs a second `InternalsVisibleTo` grant, to `"DynamicProxyGenAssembly2"`.** The usual `InternalsVisibleTo("<Module>.Tests")` is enough to let the test project *reference* an internal type, but Moq's Castle DynamicProxy builds its proxy in a dynamically generated assembly named `DynamicProxyGenAssembly2`, which separately needs visibility to implement an `internal interface`. No project needs it today (the interfaces `Identity.Tests` mocks are `public`); a project hitting the issue adds `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` rather than making the interface `public`.

## Deviations From Norms Elsewhere in the Repo

- **`Identity`'s handlers delegate to service classes.** Its command handlers forward to `IUserService`/`IRoleService` instead of holding the logic (the search query uses `UserManager<User>` directly), and its read endpoints and token endpoints call services directly. New modules put behavior on the aggregate and logic in the handler.
- **Migration projects version their own packages**, unlike every other `src/` project — see [dependency-graph.md § Version Mismatches](../architecture/dependency-graph.md#version-mismatches).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
