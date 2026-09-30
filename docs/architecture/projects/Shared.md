# Project Overview: Shared

## Purpose

`src/Shared` (assembly/namespace `StarterKit.Shared`) is the **shared kernel** and the solution's only leaf project: it references no solution project, and every other framework project, every module `.Contracts` project, and the migrators build on it. It holds the types a module needs to express its domain and its contracts without depending on hosting or persistence:

- thin DDD building blocks over the vendor `Lightsoft.SharedKernel` types (entity bases, `DomainEvent`, value objects);
- the `IntegrationEvent` base for messages that cross module boundaries;
- the ambient abstractions `ICurrentUser` and `IDateTime`, with a claims-based `CurrentUserBase`;
- permission-based authorization (policy provider, handler, super-user rule);
- the mediator pipeline behaviours;
- paging/search query records, a DTO base, claim-type and other shared constants.

Most of these types derive from or implement a vendor type (namespaces `Light.*`); the vendor type owns the core behaviour and `Shared` fixes the framework's choices on top of it.

## Public Surface

**Domain building blocks** (`Entities/`, `ValueObjects/`, root):

| Type | Role |
|---|---|
| `AuditableEntity` | Entity base over the vendor `Light.Domain.Entities.AuditableEntity`: a string `Id` generated on construction (`LightId.NewId()`), audit fields `Created`/`CreatedBy`/`LastModified`/`LastModifiedBy`, and the vendor domain-event list (`AddDomainEvent`, `DomainEvents`, `ClearDomainEvents`) |
| `AuditableEntity<TId>` | Same, over the vendor `BaseAuditableEntity<TId>`, for keys of another type (e.g. database-generated numeric keys); `Id` is not generated |
| `DomainEvent` | Abstract record over the vendor `BaseEvent`, also a `Light.Mediator.INotification`, so `Persistence` can publish it through the mediator |
| `ActiveStatus` | Owned value object holding a `State` (`Inactive`, `Active`, `Locked`) with an in-place `Update` |
| `Money`, `VatPercentage` | Guarded value objects (non-negative amount with an ISO-4217-shaped currency; a percentage in `[0, 100]`). A violation throws the vendor `ValidationException`. Their in-place `Update` is `internal` |

**Integration events**: `IntegrationEvent` — abstract record implementing the vendor `IIntegrationEvent`, with a generated `Id` (`LightId.NewId()`) and a UTC `CreationDate`. How events are published and consumed: [EventBusMassTransitRabbitMQ](EventBusMassTransitRabbitMQ.md).

**Ambient abstractions**:

| Type | Role |
|---|---|
| `ICurrentUser` | `SessionId`, `UserId`, `Username`, `IsAuthenticated`, `IsInRole`, `HasPermission` |
| `CurrentUserBase` (`Authorization/`) | Abstract `ICurrentUser` that reads everything from a settable `ClaimsPrincipal` via the claim types in `ClaimTypeConstants`; also exposes name, email, phone, and employee-id claims. `HasPermission` is true for a `permission` claim or a super user |
| `IDateTime` | `UtcNow` and `AuditTime`, both implemented as default interface members returning `DateTimeOffset.UtcNow`; callers resolve and use it through the interface |

Implementations live elsewhere: `ServerCurrentUser` and `DateTimeService` in [Infrastructure](Infrastructure.md), `MigratorCurrentUser` in [Persistence](Persistence.md).

**Authorization** (`Authorization/`):

| Type | Role |
|---|---|
| `DependencyInjection.AddPermissionPolicies` | Registers the vendor permission policy provider (as `IAuthorizationPolicyProvider`) and the vendor `IPermissionManager` |
| `DependencyInjection.AddPermissionAuthorization` | Registers the permission authorization handler |
| `SuperUserPolicy`, `AccessControl.IsFullControl` | The super-user rule: the user name `super` has full control. `IsFullControl` extends both `ICurrentUser` and `ClaimsPrincipal` |

**Mediator pipeline behaviours**:

| Type | Role |
|---|---|
| `LoggingBehaviour<TRequest, TResponse>` | Logs the request type name and elapsed milliseconds; never logs request or response bodies |
| `ValidationBehaviour<TRequest, TResponse>` | Runs every registered FluentValidation `IValidator<TRequest>`; on failure throws the vendor `ValidationException` with the errors grouped by property name |

**Queries, DTOs, results, helpers**:

| Type | Role |
|---|---|
| `PageQuery`, `SearchQuery` | Records implementing the vendor `IPage` (`PageNumber` default 1, `PageSize` default 20); `SearchQuery` adds `SearchValue`. `Persistence`'s paging extensions accept any `IPage` |
| `BaseDto`, `BaseDto<TId>` | DTO base whose `Id` serializes first |
| `AffectedRowsResult.From(int)` | Maps a row count to a vendor `Result` (success when above zero) |
| `Constants/` | `ClaimTypeConstants` (the claim names tokens and `CurrentUserBase` agree on — e.g. `uid`, `un`, `jti`, `permission`), `CronTimeConstants`, `CurrencyConstants` |
| `ClaimsPrincipalExtensions`, `CollectionSyncExtensions`, `ReflectionHelper` | Claim readers, a remove/add collection diff, and a reader of public constants on nested types |

The vendor `Result`/`Result<T>`/`Paged<T>` contracts (`Light.Contracts`) and extensions (`Light.Extensions`) are global usings inside `Shared` and reach consumers through its package references.

## Configuration

`Shared` reads no configuration. The super-user list is hard-coded in `SuperUserPolicy`.

## Design Notes

- **Permission authorization**: an endpoint names a permission as its policy (the vendor `[MustHavePermission("…")]` attribute sets `Policy`). The vendor provider first returns a normally registered policy of that name; otherwise it builds and caches a policy holding one `PermissionRequirement` for that name — any name is accepted, since `Shared`'s `PolicyProvider` does not restrict `CheckPermissionValidAsync`. The handler succeeds when the principal has a `permission` claim with that value or is a super user. A misspelled permission therefore fails closed for every user except `super`.
- **Super user**: `super` bypasses every permission check, both in the handler and in `CurrentUserBase.HasPermission`. The rule is a hard-coded list, not configuration or a claim.
- **Validation vs. domain rules**: `ValidationBehaviour` is the input-shape gate for mediator requests; the value objects guard only their own domain invariants. Both throw the vendor `ValidationException`, which the host's exception handler turns into an error response — see [architecture.md § HTTP request pipeline](../architecture.md#http-request-pipeline).
- **Domain vs. integration events**: `DomainEvent` stays in-process and is dispatched by `Persistence`; `IntegrationEvent` crosses module boundaries through `IEventBus`. The rule is in [CLAUDE.md § 7](../../../CLAUDE.md#7-framework-conventions).
- **Value objects mutated in place**: `ActiveStatus.Update` (public) and `Money`/`VatPercentage.Update` (internal) change the tracked owned instance instead of replacing it.

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Lightsoft.SharedKernel` | package | Entity, event, and value-object bases; `LightId`; vendor exceptions |
| `Lightsoft.Result`, `Lightsoft.Extensions` | package | `Result`/`Paged` contracts, `IPage`, extension helpers |
| `Lightsoft.Mediator` | package | `INotification`, `IPipelineBehavior` |
| `Lightsoft.EventBus` | package | `IIntegrationEvent`, `IEventBus` |
| `Lightsoft.AspNetCore.Authorization` | package | Permission policy provider, handler, requirement, and registration helpers |
| `FluentValidation`, `Mapster` | package | Validators for `ValidationBehaviour`; mapping (configured in `Infrastructure`) |

No project reference. Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](../dependency-graph.md).

## Depended On By

| Project | Why |
|---|---|
| `Infrastructure`, `Persistence`, `EventBusMassTransitRabbitMQ` | Framework projects built on the kernel |
| `Identity.Contracts` | Kernel types and `IntegrationEvent` for the module's seam; `Identity` and `Identity.Web` reach `Shared` through it — see [Identity](Identity.md) |
| `Notifications.Contracts` | `PageQuery` for the module's lookup DTO; `Notifications` reaches `Shared` through it — see [Notifications](Notifications.md) |
| `src/Migrations/{MSSQL,PostgreSQL,Sqlite}` | Composition of the migrate-and-seed apps |
| `tests/Framework.Tests`, `tests/Identity.Tests`, `tests/Notifications.Tests` | Unit tests |

`StarterKit.WebApi` reaches `Shared` transitively.

## Notable Conventions

- The authorization handler and policy provider are `internal`; modules use only the registration methods and `ICurrentUser`.
- `InternalsVisibleTo` grants only `Framework.Tests`.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
