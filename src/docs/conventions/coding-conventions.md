# Coding Conventions: Backend

## Build & Tooling

- Target framework: `net10.0` across every backend project.
- Central package management via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=false`). The five test projects (`tests/{Framework,Identity,Organization,Approval,LeaveManagement}.Tests`) opt out and pin their own test-package versions directly (`xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, plus `Moq` for all but `Framework.Tests`).
- `Directory.Build.props` sets `ImplicitUsings=enable` and `Nullable=enable` repo-wide.
- Module structure: flat projects directly under `src/` (no `src/Modules/` nesting), plus a `<Module>.Contracts` seam per module — see [../architecture/architecture.md § Layering](../architecture/architecture.md#layering). Five modules exist, all single-project: `Identity`, `Notifications`, `Organization`, `Approval`, `LeaveManagement` (each `<Module>.Api` + `<Module>.Contracts`). Per-module detail lives in [../architecture/modules/](../architecture/modules/).
- Vendor library family: `Lightsoft.*` (namespace `Light.*`) supplies the mediator, `Result`/`Paged` contracts, domain base types, ASP.NET Core authorization/modularity/CORS/Swagger helpers, EF Core helpers, and Serilog setup — treat as fixed external API, not renameable/refactorable project code.

## Style

- No root `.editorconfig`.
- Nullable reference types enabled everywhere; file-scoped namespaces consistently.
- PascalCase for all constants and enum members (no `SCREAMING_SNAKE_CASE`).
- Folder names mirror the trailing namespace segment (e.g. `Authorization/` ⇒ `StarterKit.Authorization`).
- CQRS command/query types: `internal sealed record` (never `public` — the controller is in the same assembly and the type is not part of any seam), one file per feature named after the feature (`CreateUser.cs`, not `CreateUserCommand.cs`), holding the command/query and its handler. A command/query wraps the `Contracts` request DTO (`CreateUserCommand(CreateUserRequest Model)`) or takes primitives for trivial payloads (`DeleteUserCommand(string Id)`); the controller binds the `Contracts` DTO and constructs the command/query.

## Structural Conventions

- **Domain design is DDD-first** — model aggregates/invariants/value objects/domain events before handlers, rules on the aggregate not the handler/service. See [../../CLAUDE.md § Design Approach](../../CLAUDE.md) and each module doc's § Notable Conventions.
- DI registration via small `static class DependencyInjection` extension classes exposing `Add<Feature>`/`Use<Feature>` methods, one per feature area/folder.
- Result pattern: vendor `Light.Contracts.Result`/`Result<T>`/`PagedResult<T>` for expected-failure outcomes; `Light.Exceptions.ValidationException` (thrown by `ValidationBehaviour<,>`) for request validation failures. **Exception to the rule**: the `ApprovalRequest` aggregate throws `Light.Exceptions.*` (`ValidationException`/`ConflictException`/`ForbiddenException`) for domain-invariant violations — `ApprovalService` maps them back to a `Result` so the `IApprovalService` seam stays `IResult`-based (see [../architecture/modules/Approval.md](../architecture/modules/Approval.md) § Notable Conventions).
- Mediator pipeline behaviors (registered in `StarterKit.WebApi/ConfigureExtensions.cs`, outermost first): `LoggingBehaviour<,>` (`src/Shared` — logs request type name + elapsed time only, never bodies), then `ValidationBehaviour<,>` (FluentValidation).
- Logging: `AppLogging` static Serilog logger for bootstrap/startup; standard `ILogger<T>` DI for request/runtime logging elsewhere.
- **CQRS handler shape, three variants** (see [../architecture/architecture.md § Key Design Patterns](../architecture/architecture.md#key-design-patterns)): every controller action dispatches an `internal` mediator command/query, but `Identity`/`Notifications` handlers forward to a service class (a half-migration — [../known-debt.md](../known-debt.md) D1), `Organization`/`LeaveManagement` handlers hold the `DbContext` logic directly, and `Approval` splits by audience (write path behind `IApprovalService` because it must be DI-reachable cross-module; read path inline).
- **Audience-split controllers**: `Notifications` established, and `Approval` follows, a split into an admin controller (explicit permissions) and a self-service controller (permission-less, hard-scoped via `ICurrentUser`/`GetEmployeeId`) over the same table.
- Every module gets a `<Module>.Contracts` seam. `Shared` is the solution's only true leaf — every `<Module>.Contracts` references `Shared` (`Identity.Contracts` only to reach a transitive package), so "seam project" does not imply "leaf project".

## Testing Conventions

- Framework: xUnit v3 (`xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) on Microsoft.Testing.Platform. On the .NET 10 SDK, if `dotnet test` refuses the legacy VSTest path, run the built test executable directly — see [../../CLAUDE.md § Testing](../../CLAUDE.md).
- Naming: `<TypeUnderTest>Tests` classes; `MethodOrMember_ShouldExpectedBehavior_WhenCondition` methods; `// Arrange`/`// Act`/`// Assert` comments.
- `Framework.Tests` uses hand-written fakes/test doubles (`RecordingPublisher : IPublisher`, `TestCurrentUser : CurrentUserBase`), no mocking library. The module test projects add `Moq` — but only for **cross-module seam interfaces** (`Identity.Tests` mocks `UserManager<User>`/`IMediator`; `Organization.Tests` mocks `IUserService`; `LeaveManagement.Tests` mocks `IOrgDirectoryService` + `IApprovalService`) — everything else runs against a real Sqlite in-memory DbContext via a per-module `TestHost` (`IdentityTestHost`, `OrganizationTestHost`, …).
- Test project layout mirrors the module's own folder structure under `tests/<Module>.Tests/<Area>/`, plus a `TestSupport/` folder. `InternalsVisibleTo` is set on each `<Module>.Api.csproj` (and `Shared`/`Infrastructure`/`Persistence` for `Framework.Tests`) so tests reach the `internal` command/query types and handlers.
- **Coverage**: `tests/{Framework,Identity,Organization,Approval,LeaveManagement}.Tests` (~69 / ~100 / ~63 / ~64 / ~30 tests). `Notifications` has **no dedicated test project yet** and `Framework.Tests` doesn't reference it — the module is currently untested ([../known-debt.md](../known-debt.md)).

## Deviations From Norms Elsewhere in the Repo

Module-specific deviations live in each module doc's § Notable Conventions ([../architecture/modules/](../architecture/modules/)); the recurring ones:

- **Undeclared transitive packages** — `Identity.Api`, `Identity.Contracts`, and `Notifications.Contracts` each use a vendor type without declaring the package, riding in via a `ProjectReference`. `Organization`/`Approval`/`LeaveManagement` (both projects each) declare everything they use — the pattern to follow. See [../architecture/dependency-graph.md](../architecture/dependency-graph.md) and [../known-debt.md](../known-debt.md).
- **`Identity` predates the convention** (adopted 2026-07-30) — informal internal layering, CQRS handlers delegating to service classes (D1). `Notifications`/`Organization`/`Approval`/`LeaveManagement` conform.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-10_
