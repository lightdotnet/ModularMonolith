# Coding Conventions: Backend

The short-form framework rules (packages, errors, API responses, DDD, events, DI, formatting, EF Core, tests) live in [CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions); this document adds the detail behind them. Project layout and dependency rules: [CLAUDE.md § 1](../../CLAUDE.md#1-repository-purpose).

## Build & Tooling

- `Directory.Build.props` sets `net10.0`, `ImplicitUsings=enable`, and `Nullable=enable` for every project.
- Central package management via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=false`). The projects that opt out — the test projects through `tests/ModuleTests.props`, the migration projects, `StarterKit.ServiceDefaults`, and `StarterKit.AppHost` — are listed in [dependency-graph.md § Version Mismatches](../architecture/dependency-graph.md#version-mismatches).
- Vendor `Lightsoft.*` (namespace `Light.*`) types are a fixed external API, not renameable/refactorable project code.

## Style

- No root `.editorconfig`.
- File-scoped namespaces consistently.
- PascalCase for all constants and enum members (no `SCREAMING_SNAKE_CASE`).
- Folder names mirror the trailing namespace segment (e.g. `src/Identity/Features/Users/Commands/` ⇒ `StarterKit.Modules.Identity.Features.Users.Commands`).
- CQRS command/query types: `internal sealed record` (never `public` — the controller is in the same assembly and the type is not part of any seam), one file per feature named after the feature (`CreateUser.cs`, not `CreateUserCommand.cs`), holding the command/query, its validator (see § Validation below), and its handler. A command/query wraps the request DTO (`CreateUserCommand(CreateUserRequest Model)`) or takes primitives for trivial payloads (`DeleteUserCommand(string Id)`); the controller binds the DTO and constructs the command/query.

## Structural Conventions

- **Domain design is DDD-first** — model aggregates, invariants, value objects, and domain events before handlers ([CLAUDE.md § 7](../../CLAUDE.md#7-framework-conventions)). The use-case logic lives in the mediator handler, which loads the aggregate, calls its behaviour, and saves; a module has no internal service between handler and aggregate. Infrastructure adapters (SignalR push, SMTP mail) stay services. `Notifications` is the reference — see [../architecture/projects/Notifications.md § Design Notes](../architecture/projects/Notifications.md#design-notes).
- **DI registration**: exceptions to the `DependencyInjection` naming convention are noted in each project's overview under Notable Conventions (e.g. the host composes through `ConfigureExtensions` — see [../architecture/projects/WebApi.md § Notable Conventions](../architecture/projects/WebApi.md#notable-conventions)).
- **Validation — two-layer FluentValidation.** The `ValidationBehaviour<,>` pipeline behavior and the host's validator registration are in place; no project defines a validator yet. The shape to follow: each request DTO gets an `AbstractValidator<TRequest>` **in the same file**, holding field-shape rules only (`NotEmpty`, `MaximumLength`, `IsInEnum`); each mediator command gets a thin `AbstractValidator<TCommand>` **in the same file as the command+handler**, validating route-level primitives directly (e.g. `RuleFor(x => x.Id).NotEmpty()`) and delegating the DTO via `RuleFor(x => x.Model).SetValidator(new XRequestValidator())`. Queries generally don't need a validator.
- **Logging**: `AppLogging` ([Infrastructure](../architecture/projects/Infrastructure.md)) only for bootstrap/startup; standard `ILogger<T>` DI for request/runtime logging everywhere else.
- **Audience-split controllers**: an admin controller and a self-service controller hard-scoped via `ICurrentUser` over the same data — `Identity`'s `UserController` and `UserProfileController`, `Notifications`' `NotificationController` and `UserNotificationController`.
- **Seam vs. leaf**: a `<Module>.Contracts` seam references `Shared`, so "seam project" does not imply "leaf project"; `Shared` is the only leaf.
- **Cross-module communication**:
  - Prefer an **integration event** for a fire-and-forget reaction to another module's change — `Notifications` consumes `Identity`'s `UserProvisionedIntegrationEvent` to send the welcome mail. Publishing and consuming: [../architecture/projects/EventBusMassTransitRabbitMQ.md](../architecture/projects/EventBusMassTransitRabbitMQ.md).
  - Use the owning module's **`.Contracts` facade** (`I<Module>ModuleApi`) for a synchronous call: a DI-only interface on the `.Contracts` project, implemented `internal` in the module, injected by constructor.
  - The facade is a **thin mediator dispatcher**, not a logic-bearing service: each member sends the module's own command/query through `IMediator`. Commands and queries stay `internal` to the module and never appear in `.Contracts`. `Notifications`' `NotificationsModuleApi` is the reference.
  - A facade call bypasses the controllers' permission attributes; the calling module authorizes the operation before it calls the facade.
- **Filtered indexes** are declared with `HasProviderFilter` — see [migrations.md § Provider-aware filtered indexes](migrations.md#provider-aware-filtered-indexes).

## Testing Conventions

- Framework: xUnit v3 + Moq, pinned by `tests/ModuleTests.props`; how to run them: [development-guide.md § Running Tests](development-guide.md#running-tests).
- Coverage: `tests/Framework.Tests` covers the framework projects; each module has one `tests/<Module>.Tests` project (`tests/Identity.Tests`, `tests/Notifications.Tests`).
- Layout: a test project mirrors its source projects' folder structure — `tests/Framework.Tests/<ProjectName>/...` for the framework projects and `tests/<Module>.Tests/<Area>/...` for a module; a module test project adds a `TestSupport/` folder for shared test infrastructure.
- Naming: `<TypeUnderTest>Tests` classes; `MethodOrMember_ShouldExpectedBehavior_WhenCondition` methods; `// Arrange`/`// Act`/`// Assert` comments.
- Doubles: `Framework.Tests` mostly uses hand-written private fakes (e.g. a recording `IPublisher`, a `CurrentUserBase` subclass), with `Moq` where a logger or similar dependency needs a double, and a Sqlite in-memory `DbContext` for persistence behavior. The module test projects use `Moq` for the module's services and framework dependencies, hand-written `FakeCurrentUser`/`FakeDateTime`, and run persistence-dependent tests against a real Sqlite in-memory `DbContext` via a test host in `TestSupport/` (`IdentityTestHost`, `NotificationsTestHost`). `Notifications.Tests` runs its handlers against the real context with a mocked `IHubService`.
- Internal access: a source project grants `InternalsVisibleTo` to its test project so tests reach `internal` types.
- **Mocking an `internal` interface with Moq needs a second `InternalsVisibleTo` grant, to `"DynamicProxyGenAssembly2"`.** The usual `InternalsVisibleTo("<Module>.Tests")` is enough to let the test project *reference* an internal type, but Moq's Castle DynamicProxy builds its proxy in a dynamically generated assembly named `DynamicProxyGenAssembly2`, which separately needs visibility to implement an `internal interface`. `Notifications` grants it so its tests can mock the internal `IHubService`; a project hitting the issue adds `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` rather than making the interface `public`.

## Deviations From Norms Elsewhere in the Repo

- **`Identity`'s handlers delegate to service classes.** Its command handlers forward to `IUserService`/`IRoleService` instead of holding the logic (the search query uses `UserManager<User>` directly), its read endpoints and token endpoints call services directly, and its `IIdentityModuleApi` implementation reads the context and calls services instead of dispatching mediator requests. Other modules follow the handler-centric rule under § Structural Conventions.
- **Migration projects, `StarterKit.ServiceDefaults`, and `StarterKit.AppHost` version their own packages**, unlike every other `src/` project — see [dependency-graph.md § Version Mismatches](../architecture/dependency-graph.md#version-mismatches).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
