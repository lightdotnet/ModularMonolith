# Dependency Graph: Backend

## Package References

Package versions are centrally managed via the root `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`) — individual `.csproj` files reference packages by name only, with no `Version` attribute. Most `Lightsoft.*` packages share one `$(LightVersion)` property; `Lightsoft.Mediator`, `Lightsoft.Result`, and `Lightsoft.EventBus`/`Lightsoft.EventBus.MassTransit.RabbitMQ` are pinned separately. Most `Microsoft.AspNetCore.*`/EF Core packages share `$(AspnetVersion)`. `tests/Framework.Tests`, `tests/Identity.Tests`, `tests/Organization.Tests`, `tests/Approval.Tests`, `tests/LeaveManagement.Tests`, `tests/Location.Tests`, `tests/Catalog.Tests`, `tests/Orders.Tests`, and `tests/Inventory.Tests` opt out (`ManagePackageVersionsCentrally=false`) and pin their own versions. Exact numbers are intentionally omitted below — check `Directory.Packages.props` (and the nine test `.csproj` files).

| Project | Packages | Notes |
|---|---|---|
| Shared | FluentValidation, Lightsoft.AspNetCore.Authorization, Lightsoft.EventBus, Lightsoft.Extensions, Lightsoft.Mediator, Lightsoft.Result, Lightsoft.SharedKernel, Mapster | `Lightsoft.EventBus` has no usage found in `Shared` — see `../known-debt.md` (dependency hygiene). |
| Infrastructure | AspNetCore.HealthChecks.UI.Client, Lightsoft.AspNetCore.Extensions, Lightsoft.AspNetCore.Modularity, Lightsoft.FileGenerator, Lightsoft.Serilog | `Lightsoft.FileGenerator` has no usage found here either — see `../known-debt.md`. |
| Persistence | Lightsoft.Caching, Lightsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.InMemory, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.SqlServer, Npgsql.EntityFrameworkCore.PostgreSQL, SQLitePCLRaw.lib.e_sqlite3 | One EF Core provider package per supported `DbProvider` value; `SQLitePCLRaw.lib.e_sqlite3` is a transitive of `Microsoft.EntityFrameworkCore.Sqlite`. `Lightsoft.Caching` (added this session) backs the new opt-in `Repositories/ICacheRepository<T>`/`IDynamicsDbCache<T>` cache-repository layer — see [architecture.md § Shared Kernel](architecture.md#shared-kernel--common-building-blocks) and `../known-debt.md`. |
| Identity.Contracts | Lightsoft.Mediator, Lightsoft.Result | `Lightsoft.Mediator` for `INotification` on the integration events; `Lightsoft.Result` for the service return types. The `Lightsoft.AspNetCore.Authorization` types used by `IdentityPermissionProvider` still ride in transitively via `Shared` — see `../known-debt.md`. |
| Identity.Api | Lightsoft.ActiveDirectory, Lightsoft.SharedKernel, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.Extensions.Identity.Core | Several vendor types it uses (`Lightsoft.AspNetCore.Authorization`, `Lightsoft.Result`, `Lightsoft.Extensions`, `Lightsoft.Mediator`, `Light.EntityFrameworkCore.Extensions.WhereIf`) ride in transitively via `ProjectReference`s rather than being declared directly — see `../known-debt.md`. |
| Identity.Web | FluentValidation.DependencyInjectionExtensions, Microsoft.AspNetCore.Authentication.OpenIdConnect | Razor Pages login host inside the Identity module. `OpenIdConnect` backs the Microsoft Entra ID external-login scheme; `FluentValidation.DependencyInjectionExtensions` registers validators in the standalone-host composition. |
| Notifications.Contracts | (none — no direct `<PackageReference>`) | The `Light.AspNetCore.Authorization` types it uses ride in transitively via its `ProjectReference` to `Shared`. |
| Notifications.Api | Lightsoft.SmtpMail | Its only direct package reference; everything else (`Light.EntityFrameworkCore.Extensions`, `Light.Specification`, `Mapster`, `Microsoft.AspNetCore.SignalR`) rides in transitively. |
| Organization.Contracts | Lightsoft.AspNetCore.Authorization | Declared directly. |
| Organization.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Mapster | Every vendor package it directly uses is declared directly. |
| Approval.Contracts | Lightsoft.AspNetCore.Authorization, Lightsoft.Mediator, Lightsoft.Result | All declared directly. `Lightsoft.Mediator` for `INotification` on `ApprovalFinalizedIntegrationEvent`. |
| Approval.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Lightsoft.SharedKernel, Mapster | Every vendor package it directly uses is declared directly. `Lightsoft.SharedKernel` for `Light.Exceptions.*`, thrown by the `ApprovalRequest` aggregate and mapped back to `Result` in `ApprovalService`. |
| LeaveManagement.Contracts | Lightsoft.AspNetCore.Authorization, Lightsoft.Result | Declared directly. |
| LeaveManagement.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Mapster | Every vendor package it directly uses is declared directly. `LeaveRequestReconciliationService` derives from `BackgroundService` (`Microsoft.Extensions.Hosting.Abstractions`), which rides in via the ASP.NET Core shared framework — not a declared package. |
| Location.Contracts | Lightsoft.AspNetCore.Authorization | Declared directly. |
| Location.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.Caching, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Mapster | Every vendor package it directly uses is declared directly. `Lightsoft.Caching` backs `ILocationTypeCache`'s `Light.Extensions.Caching.ICacheService` dependency. |
| Catalog.Contracts | Lightsoft.AspNetCore.Authorization | Declared directly. |
| Catalog.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Mapster | Every vendor package it directly uses is declared directly. |
| Orders.Contracts | Lightsoft.AspNetCore.Authorization, Lightsoft.Mediator | Declared directly. `Lightsoft.Mediator` for `INotification` on `OrderPlacedIntegrationEvent`. Also carries `ProjectReference`s to `Catalog.Contracts`/`Location.Contracts` that no type in this project actually uses — see [modules/Orders.md § Dependencies](modules/Orders.md#dependencies). |
| Orders.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result, Mapster | Every vendor package it directly uses is declared directly. `Mapster` is declared but currently unused (query handlers hand-map to DTOs instead). Also references `Inventory.Contracts` (`ProjectReference`), consumed by `IInventoryService` — see [modules/Orders.md § Dependencies](modules/Orders.md#dependencies). |
| Inventory.Contracts | Lightsoft.AspNetCore.Authorization | Declared directly. |
| Inventory.Api | Lightsoft.AspNetCore.Authorization, Lightsoft.EntityFrameworkCore, Lightsoft.Mediator, Lightsoft.Result | Every vendor package it directly uses is declared directly. Also references `Location.Contracts` (`ProjectReference`), consumed by `InventoryService`/`RecordStockMovementCommandHandler` via `ILocationDirectoryService` — see [modules/Inventory.md § Dependencies](modules/Inventory.md#dependencies). |
| StarterKit.WebApi | AspNetCore.HealthChecks.UI.Client, FluentValidation.DependencyInjectionExtensions, Lightsoft.AspNetCore.Extensions, Lightsoft.AspNetCore.Swagger, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.VisualStudio.Azure.Containers.Tools.Targets, Spectre.Console | `Microsoft.AspNetCore.Authentication.JwtBearer` backs the host-owned Bearer + `"HubBearer"` schemes in `Authentication/ApiAuthenticationExtensions`. Uses `Lightsoft.Serilog` without declaring it (rides in via `Infrastructure`). |
| Framework.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | Opts out of central package management. |
| Identity.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Same opt-out, plus `Moq`. |
| Organization.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Mocks `Identity.Contracts.Services.IUserService` in the employee-login tests; else runs against a real Sqlite in-memory `OrganizationDbContext`. |
| Approval.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Same opt-out/package set. |
| LeaveManagement.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Mocks `IOrgDirectoryService` and `IApprovalService`; else runs against a real Sqlite in-memory `LeaveManagementDbContext`. |
| Location.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Mocks `ILocationTypeCache` (an `internal` interface — see `../known-debt.md`/`modules/Location.md` for the `InternalsVisibleTo("DynamicProxyGenAssembly2")` fix this needed); else runs against a real Sqlite in-memory `LocationDbContext`. |
| Catalog.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Runs against a real Sqlite in-memory `CatalogDbContext`; `Moq` is referenced but has no usage found anywhere in the project. |
| Orders.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Mocks `ICatalogPricingService`/`ILocationDirectoryService`/`IInventoryService` in the command-handler tests; else runs against a real Sqlite in-memory `OrdersDbContext`. |
| Inventory.Tests | xunit.v3, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq | Mocks `ILocationDirectoryService` (an `internal`-reachable type via `Inventory.Api`'s own `ProjectReference` to `Location.Contracts` — this test project carries no direct `ProjectReference` to `Location.Contracts` itself); else runs against a real Sqlite in-memory `InventoryDbContext`. |

The undeclared-transitive-dependency pattern (a project using a vendor type without declaring the package, riding in via a `ProjectReference`) still recurs in `Identity.Api`, `Identity.Contracts` (for `Lightsoft.AspNetCore.Authorization` only — `Mediator`/`Result` are declared), and `Notifications.Contracts`. `Organization`/`Approval`/`LeaveManagement`/`Location`/`Catalog`/`Orders`/`Inventory` (both `.Contracts` and `.Api`) don't repeat it — `Approval.Contracts` now declares `Lightsoft.Mediator` directly for its integration event. See `../known-debt.md`.

## Circular References

None found. `Shared` is the only true leaf (no `ProjectReference`s). Every module's `Contracts` project references `Shared`, so none of them is a true leaf either. Dependency direction is one-way throughout: `Api`/`Contracts` projects → `Infrastructure`/`Persistence` → `Shared`; `Identity.Web` → `Identity.Api` (intra-module); and `StarterKit.WebApi` (composition-root host) → all nine business modules plus `Identity.Web`. No project-reference cycle exists anywhere.

```text
Infrastructure -> Shared
Persistence -> Shared
Identity.Contracts -> Shared
Identity.Api -> Identity.Contracts
Identity.Api -> Infrastructure
Identity.Api -> Persistence
Identity.Web -> Identity.Api
Identity.Web -> Infrastructure
Notifications.Contracts -> Shared
Notifications.Api -> Notifications.Contracts
Notifications.Api -> Infrastructure
Notifications.Api -> Persistence
Notifications.Api -> Identity.Contracts
Organization.Contracts -> Shared
Organization.Api -> Organization.Contracts
Organization.Api -> Infrastructure
Organization.Api -> Persistence
Organization.Api -> Identity.Contracts
Approval.Contracts -> Shared
Approval.Api -> Approval.Contracts
Approval.Api -> Infrastructure
Approval.Api -> Persistence
Approval.Api -> Notifications.Contracts
LeaveManagement.Contracts -> Shared
LeaveManagement.Api -> LeaveManagement.Contracts
LeaveManagement.Api -> Infrastructure
LeaveManagement.Api -> Persistence
LeaveManagement.Api -> Approval.Contracts
LeaveManagement.Api -> Organization.Contracts
Location.Contracts -> Shared
Location.Api -> Location.Contracts
Location.Api -> Infrastructure
Location.Api -> Persistence
Catalog.Contracts -> Shared
Catalog.Api -> Catalog.Contracts
Catalog.Api -> Infrastructure
Catalog.Api -> Persistence
Orders.Contracts -> Shared
Orders.Api -> Orders.Contracts
Orders.Api -> Infrastructure
Orders.Api -> Persistence
Orders.Api -> Catalog.Contracts
Orders.Api -> Location.Contracts
Orders.Api -> Inventory.Contracts
Inventory.Contracts -> Shared
Inventory.Api -> Inventory.Contracts
Inventory.Api -> Infrastructure
Inventory.Api -> Persistence
Inventory.Api -> Location.Contracts
StarterKit.WebApi -> Identity.Api
StarterKit.WebApi -> Identity.Web
StarterKit.WebApi -> Notifications.Api
StarterKit.WebApi -> Notifications.Contracts
StarterKit.WebApi -> Organization.Api
StarterKit.WebApi -> Approval.Api
StarterKit.WebApi -> LeaveManagement.Api
StarterKit.WebApi -> Location.Api
StarterKit.WebApi -> Catalog.Api
StarterKit.WebApi -> Orders.Api
StarterKit.WebApi -> Inventory.Api
StarterKit.WebApi -> Infrastructure
StarterKit.WebApi -> Shared
Framework.Tests -> Shared
Framework.Tests -> Infrastructure
Framework.Tests -> Persistence
Identity.Tests -> Identity.Api
Identity.Tests -> Shared
Organization.Tests -> Organization.Api
Organization.Tests -> Identity.Contracts
Organization.Tests -> Shared
Approval.Tests -> Approval.Api
Approval.Tests -> Approval.Contracts
Approval.Tests -> Shared
LeaveManagement.Tests -> LeaveManagement.Api
LeaveManagement.Tests -> LeaveManagement.Contracts
LeaveManagement.Tests -> Approval.Contracts
LeaveManagement.Tests -> Organization.Contracts
LeaveManagement.Tests -> Shared
Location.Tests -> Location.Api
Location.Tests -> Shared
Catalog.Tests -> Catalog.Api
Catalog.Tests -> Shared
Orders.Tests -> Orders.Api
Orders.Tests -> Shared
Inventory.Tests -> Inventory.Api
Inventory.Tests -> Inventory.Contracts
Inventory.Tests -> Shared
```

Note: `src/Migrations/MSSQL` also references `Location.Api`, `Catalog.Api`, `Orders.Api`, and
`Inventory.Api` directly (for their respective `DbContext`/`ContextInitialiser` pairs), same as every
other module's MSSQL migrations project — the Migrations projects are omitted from the diagram above,
consistent with the rest of this file. `Location`, `Catalog`, `Orders`, and `Inventory` are currently
the only modules **not** also referenced from the `PostgreSQL`/`Sqlite` migrations projects — see
[modules/Location.md § Data Access](modules/Location.md#data-access) /
[modules/Catalog.md § Data Access](modules/Catalog.md#data-access) /
[modules/Orders.md § Data Access](modules/Orders.md#data-access) /
[modules/Inventory.md § Data Access](modules/Inventory.md#data-access).

## Cross-Module Boundary Violations (backend only)

None found. `Identity.Web → Identity.Api` is a direct project reference into another project's internals, but both projects belong to the **same module** (the Identity bounded context), so it is not a cross-module edge.

Nine business-module-to-business-module dependencies exist, all compliant (each reaches only the other module's `Contracts` seam):

- `Notifications.Api` references `Identity.Contracts`, consumed by `UserCreatedIntegrationEventHandler`/`ExternalUserProvisionedIntegrationEventHandler` to send a (SSO-)welcome email in reaction to the `UserCreatedIntegrationEvent` / `ExternalUserProvisionedIntegrationEvent` that Identity publishes. **This edge replaced the former `Identity.Api → Notifications.Contracts` edge** — the welcome-mail side effect moved from Identity into Notifications, and the direction flipped.
- `Organization.Api` references `Identity.Contracts`, consumed by the employee-login command handlers via `IUserService` (create/link an Identity login, store `User.Id` as an opaque string on `Employee.UserId`) and `IUserService.SetClaimAsync` (stamp/clear the `employee_id` claim).
- `Approval.Api` references `Notifications.Contracts`, consumed by `ApprovalStepPendingEventHandler`/`ApprovalFinalizedEventHandler`/`ApprovalRequestCancelledEventHandler` via `INotificationService.SendAsync`.
- `LeaveManagement.Api` references `Approval.Contracts`, consumed by the command handlers via `IApprovalService` (`CreateAsync`/`CancelAsync`, and the `GetStatus*` lean lookups) **and** by `ApprovalFinalizedIntegrationEventHandler`, which handles the `ApprovalFinalizedIntegrationEvent` `INotification` declared in `Approval.Contracts`.
- `LeaveManagement.Api` references `Organization.Contracts`, consumed by the same command handlers via `IOrgDirectoryService`.
- `Orders.Api` references `Catalog.Contracts`, consumed by `AddOrderLineCommandHandler` via `ICatalogPricingService.GetPriceInfoAsync` to resolve a product's current name/price/VAT rate/status when adding a line. **This is `Catalog`'s first cross-module consumer.**
- `Orders.Api` references `Location.Contracts`, consumed by `CreateOrderCommandHandler` via `ILocationDirectoryService.ExistsAsync` to validate `LocationId` before creating an order. **This is `Location`'s first cross-module consumer.**
- `Orders.Api` references `Inventory.Contracts`, consumed synchronously and in-process by `PlaceOrderCommandHandler`/`CancelOrderCommandHandler` via `IInventoryService.DecrementForOrderAsync`/`RestoreForOrderAsync` — **not** through an integration event, a deliberate departure from the event-based pattern below (see [modules/Orders.md](modules/Orders.md) / [modules/Inventory.md](modules/Inventory.md)).
- `Inventory.Api` references `Location.Contracts`, consumed by `InventoryService.DecrementForOrderAsync` and `RecordStockMovementCommandHandler` via `ILocationDirectoryService.ExistsAsync` to validate `LocationId` before any stock movement. **This is `Location`'s second cross-module consumer.**

`Approval.Contracts` also defines `ApprovalFinalizedIntegrationEvent` (an `INotification`), published by `ApprovalService` immediately after a decide commits to a terminal `Approved`/`Rejected` state (a requester-initiated cancellation is driven by the owning module and is **not** re-published). This is now a **live cross-module mediator-notification edge**: `LeaveManagement.Api`'s `ApprovalFinalizedIntegrationEventHandler` (`INotificationHandler<T>`, auto-registered by `AddMediatorFromAssemblies`) subscribes in-process to reconcile the local `LeaveRequest.Status`, backed by the module's own `LeaveRequestReconciliationService` (`BackgroundService`) for a dropped delivery. `LeaveManagement.Api`'s read paths no longer call `IApprovalService` at all. `Orders.Contracts` similarly defines `OrderPlacedIntegrationEvent`, published by `PlaceOrderCommandHandler` after a placement commits, but it has no subscriber in any module — including `Inventory`, which deliberately uses the synchronous `IInventoryService` seam above instead (see [modules/Inventory.md § Notable Conventions](modules/Inventory.md#notable-conventions)).

None of the reverse directions exist. `Identity.Api` now references **no** other business module.
`Orders.Contracts` also carries `ProjectReference`s to `Catalog.Contracts` and `Location.Contracts`,
but no type in `Orders.Contracts` itself uses either — the real dependency is one layer down, in
`Orders.Api` (the edges above). `Inventory.Api` has no reference back to `Orders.Api`/`Orders.Contracts`
— the dependency between the two modules runs one way only. See the per-module docs for full detail.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-20_
