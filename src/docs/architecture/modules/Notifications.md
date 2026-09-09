# Module Overview: Notifications

## Purpose

Owns storage and real-time delivery of system/user notifications: persisting notification records (sender, recipient, title, message, optional deep-link URL, read/archived status) and pushing them live to connected clients over SignalR. Exposes two HTTP surfaces: an admin-facing "browse + send" capability gated by explicit permissions, and a self-service "my notifications" surface auto-scoped to the calling user via `ICurrentUser`. Also carries a "force logout" push message that reuses the same SignalR channel as a live session-invalidation signal, with no stored record.

Beyond HTTP, the module exposes two in-process capabilities through its `Contracts` seam:

- `INotificationService.SendAsync` — combined persist-and-push, consumed cross-module by `Approval.Api`.
- `IMailService` — outbound SMTP mail. Its only consumers today are this module's own welcome-mail handlers, which react to Identity's `UserCreatedIntegrationEvent` / `ExternalUserProvisionedIntegrationEvent` (see Depended On By).

## Internal Layering

Notifications is a **single-project module** (not split Domain/Application/Infrastructure/Api) — the template's 4-row project table doesn't apply as-is; adapted to the module's actual two projects:

| Project | Responsibility | Notes |
|---|---|---|
| `Notifications.Contracts` | DTOs (`NotificationDto`), enum (`NotificationStatus`: `None`/`Read`/`Archived`, serialized by name), request shape (`NotificationLookup : PageQuery`), push-message contracts (`SystemMessage`, `ForceLogoutMessage`, both `: INotificationMessage`), service interfaces (`INotificationService` — including `SendAsync` —, `IMailService` — `SendFromSystemAsync` / `SendAsync`), permission strings + catalog (`NotificationPermissions`, `NotificationPermissionProvider`), and `NotificationConstants` — a `static class` holding `HubPath` (`"/signalr-hub"`) and `ServerNotification` (`"server-notification"`). The module's only seam project; depends only on `Shared`. | |
| `Notifications.Api` | Single project organised by folder: `Entities/Notification.cs` (`: AuditableEntity`), `Data/NotificationDbContext.cs`, `Application/Notifications/{Commands,Queries}` (each an `internal sealed record` + thin pass-through handler over `INotificationService`/`IHubService`), `Application/Users/EventHandlers/` (`UserCreatedIntegrationEventHandler`, `ExternalUserProvisionedIntegrationEventHandler` — both send a welcome email via `IMailService`, failures logged not surfaced), `Services/NotificationService.cs` + `Services/MailService.cs` (both `internal`), `Controllers/{NotificationController,UserNotificationController}.cs`, `SignalR/{SignalRHub,IHubService,HubService,CustomIdProvider,SignalRModule}.cs`, `NotificationModule.cs`. | `.Api` suffix kept for the same future-microservice-extraction reason as `Identity.Api`. |

## Public Contract

`NotificationController` (admin surface, route base `notification`, permission-gated):

| Route | Verb | Permission | Request | Response |
|---|---|---|---|---|
| `api/v{version}/notification` | GET | `notification.read` | Query `NotificationLookup` (`ToUserId?`, `Status?`, `PageNumber`/`PageSize`) | `PagedResult<NotificationDto>` |
| `api/v{version}/notification` | POST | `notification.send` | Query `fromUserId`, `fromName?`, `toUserId`; body `SystemMessage { Title, Message?, Url?, ByMessage }` | `Ok()` — persists a row, then pushes it live to `toUserId` as event `SystemMessage` |
| `api/v{version}/notification/force_logout` | POST | `notification.send` | Body `ForceLogoutMessage { UserId }` | `Ok()` — pushes a `ForceLogoutMessage` event to that user's live connections; **no DB write** |

`UserNotificationController` (self-service surface, route base `user_notification`, **no `[MustHavePermission]`** — force-scoped server-side to `ICurrentUser.UserId`):

| Route | Verb | Request | Response |
|---|---|---|---|
| `api/v{version}/user_notification` | GET | Query `NotificationLookup` — `ToUserId` is overwritten with the caller's id | `PagedResult<NotificationDto>` |
| `api/v{version}/user_notification/{entryId}` | GET | Route param `entryId` | `NotificationDto?` — **side effect**: marks the entry `Read` before returning it |
| `api/v{version}/user_notification/count_unread` | GET | none | `int` — count of the caller's `Status == None` rows |

Every action dispatches through a mediator command/query under `Application/Notifications/`; controllers only bind the `Contracts` DTO/route param and call `Mediator.Send`. `SendNotificationCommandHandler` is a thin pass-through to `INotificationService.SendAsync` (persist + push composed inside the service); `ForceLogoutCommandHandler` calls `IHubService` directly. Same CQRS-entrypoint shape as `Identity` — [known-debt.md](../../known-debt.md) D1.

**SignalR**: `SignalREndpoint` maps `NotificationConstants.HubPath` → `SignalRHub : Hub` (`[Authorize]`), with `Transports = WebSockets` and `CloseOnAuthenticationExpiration = true`. `CustomIdProvider : IUserIdProvider` maps a connection to a user id via the `ClaimTypeConstants.UserId` claim so `IHubService`'s `Clients.User(userId)` targeting works. The hub defines **no client→server methods** — it is push-only; `OnConnected`/`OnDisconnected` just join/leave a broadcast group nothing currently broadcasts to.

The browser opens this WebSocket **directly against the backend** — the admin client no longer proxies it same-origin (its `next.config.ts` has no `rewrites()`), so the backend must allow CORS for the client origin. The handshake is authenticated with a **short-lived, hub-audience-only token** minted by Identity's `POST auth/token/hub` (not the full session JWT). The co-host flows that token's expiry into the auth ticket, so `CloseOnAuthenticationExpiration` actually tears a live connection down when the ~120s token lapses; the browser re-mints per (re)connect. See [modules/Identity.md § Authentication Wiring](Identity.md#authentication-wiring) and [docs/integration.md](../../../../docs/integration.md).

**Mail (not HTTP-exposed)**: `IMailService` is consumed in-process only. `MailService` (`internal`) implements it over vendor `ISmtpMailSender`/`SmtpMailKitOptions` (namespace `Light.Smtp`, package `Lightsoft.SmtpMail`): `SendFromSystemAsync` sends from `options.UserName` labelled `"System"` (a placeholder address per an inline comment); `SendAsync` from an explicit `from`. Current callers: this module's `UserCreatedIntegrationEventHandler` and `ExternalUserProvisionedIntegrationEventHandler`.

**In-app notification push (not HTTP-exposed)**: `INotificationService.SendAsync(fromUserId, fromName, toUserId, SystemMessage, ct)` composes `SaveAsync` (persist) then `IHubService.SendAsync` (push) in one call. Consumers: `SendNotificationCommandHandler` (in-module) and `Approval.Api`'s `ApprovalStepPendingEventHandler`/`ApprovalFinalizedEventHandler` (cross-module).

**Two verified gaps**: `INotificationService.ReadAllAsync(userId)` exists but no controller/command exposes it (no "mark all read"). No server-side path ever sets `Status = Archived` despite the enum value and the admin "Archived" tab — currently unreachable from any wired endpoint.

## Data Access

`NotificationDbContext : BaseDbContext`, default schema `"system"`, one `DbSet<Notification>` → table `Notifications`. Registered via `AddConfiguredDbContext<NotificationDbContext>(configuration, DbConnectionNames.Identity)` — `DbConnectionNames.Identity` aliases `DbConnectionNames.Default`, so this module **shares the same physical database as `Identity`** (and any module using `Default`), separated by schema + table name. Only index: `HasIndex(x => x.ToUserId)` — no index on `Status` despite both list queries and `CountUnreadAsync` filtering by it ([known-debt.md](../../known-debt.md)). Column lengths: `FromUserId`/`ToUserId` `MaxLength(450)`, `FromName` `MaxLength(200)`, `Title` `MaxLength(250)`; `Message`/`Url` unconstrained. `Notification : AuditableEntity`, audited via `SaveChanges[Async]` → `AuditEntries(..., enableSoftDelete: false)` — `Notification` doesn't implement `ISoftDelete`, so this is just "no soft-delete support", not a bug. Only MSSQL has a `Notifications` migration. Mail-sending is stateless.

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Notifications.Contracts → Shared`) | Base `PageQuery`, etc. |
| `Infrastructure` | project (`Notifications.Api → Infrastructure`) | `VersionedApiController`, `AppModule`/`AppModuleEndpoint`. |
| `Persistence` | project (`Notifications.Api → Persistence`) | `BaseDbContext`, `AddConfiguredDbContext`, audit/paging extensions. |
| `Notifications.Contracts` | project (`Notifications.Api → Notifications.Contracts`) | The module's own seam. |
| `Identity.Contracts` | project (`Notifications.Api → Identity.Contracts`) | `UserCreatedIntegrationEvent` / `ExternalUserProvisionedIntegrationEvent` (the welcome-mail handlers). A cross-module edge reaching only Identity's `Contracts` seam. |
| Vendor `Light.AspNetCore.Authorization` | package, transitive (rides in via `Shared`, undeclared — [known-debt.md](../../known-debt.md)) | `[MustHavePermission]`, `IPermissionDefinitionProvider`. |
| Vendor `Light.EntityFrameworkCore.Extensions`, `Light.Specification`, `Mapster` | package | `WhereIf`, `ProjectToType<T>`, `ToPagedResultAsync` in `NotificationService`. |
| `Microsoft.AspNetCore.SignalR` | ASP.NET Core shared framework | `Hub`, `IHubContext<T>`, `IUserIdProvider`. |
| Vendor `Lightsoft.SmtpMail` (`Notifications.Api`) | package, **direct** | `ISmtpMailSender`/`SmtpMailKitOptions` backing `MailService`. The module's only direct `<PackageReference>`. |

**Notifications still never references `Identity.Api`** — `fromUserId`/`toUserId` are opaque strings, no FK or validation. The new `Notifications.Api → Identity.Contracts` edge is one-directional (welcome mail); Identity does not reference anything of Notifications.

## Depended On By

- `Approval.Api` references `Notifications.Contracts` — `ApprovalStepPendingEventHandler`/`ApprovalFinalizedEventHandler` call `INotificationService.SendAsync` to notify the approver/requester.
- `Notifications.Api` is otherwise referenced only by `StarterKit.WebApi` and `src/Migrations/MSSQL/MSSQL.csproj`.
- Client-side: `clients/admin/src/modules/notifications/` calls `notification`/`user_notification` over HTTP and opens a browser-direct WebSocket to `/signalr-hub`. The admin client has no path to `IMailService`/`INotificationService.SendAsync` — both are in-process C# capabilities.

`Identity.Api` **no longer references `Notifications.Contracts`** — the welcome-email side effect it used to own now lives in this module's `Application/Users/EventHandlers/`, reached via the `UserCreatedIntegrationEvent` published on Identity's `Contracts` seam.

## Notable Conventions

- **CQRS is the entrypoint for every controller action**, but every handler is a thin pass-through to `INotificationService`/`IHubService` — same open D1 decision as `Identity`.
- **Welcome mail reacts to Identity integration events, not a direct call.** `Application/Users/EventHandlers/` holds `INotificationHandler<UserCreatedIntegrationEvent>` and `INotificationHandler<ExternalUserProvisionedIntegrationEvent>`; both send via `IMailService` and swallow/log send failures. This is the current owner of the "send a welcome email on user creation" behaviour.
- Unlike `Identity`, this module's controllers split by **audience** (`NotificationController` admin / `UserNotificationController` self-service) rather than by resource — a pattern worth reusing.
- `NotificationConstants.ServerNotification` is referenced only by `HubService.NotifyAsync` (the broadcast overloads), which itself has **zero callers** anywhere — dead capability. The wired `SendAsync<T>` push path uses `typeof(T).Name` (`"SystemMessage"`, `"ForceLogoutMessage"`) as the event name instead.
- Two verified functional gaps: no "mark all read" endpoint despite `ReadAllAsync`; no code path ever sets `NotificationStatus.Archived`.
- `MailService.SendFromSystemAsync` sends from `options.UserName` labelled `"System"` — a placeholder per its inline comment, not a dedicated no-reply address.
- `NotificationModule.AddSmtpMail` reads the `SmtpMail` section and throws at startup if the section is missing entirely (not if individual values are blank — `appsettings.json`'s `UserName`/`Password` are empty strings, which passes this check but would fail at real send time).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-09_
