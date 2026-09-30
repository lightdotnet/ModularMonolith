# Project Overview: Notifications

The Notifications module is two projects forming one bounded context:

| Project | Assembly | Role |
|---|---|---|
| `src/Notifications.Contracts` | `StarterKit.Modules.Notifications.Contracts` | The cross-module seam — the only Notifications project another module may reference |
| `src/Notifications` | `StarterKit.Modules.Notifications` | The module implementation |

The module has no `.Web` project.

## Purpose

The module stores per-user notifications and delivers them live, and sends email:

- a `Notification` aggregate (sender, recipient, title, message, URL, status) in the module's own EF Core context;
- real-time push to connected clients over a SignalR hub;
- email over SMTP;
- the welcome mail sent when the Identity module provisions a user, driven by Identity's integration event;
- notification endpoints for the JSON API and a permission catalog registered with the framework's permission authorization.

## Public Surface

**`Notifications.Contracts`**:

| Type | Role |
|---|---|
| `INotificationsModuleApi` | In-process seam other modules use to send a notification to a user (`SendAsync`): the notification is stored, then pushed live to the recipient |
| `IMailService` | Sends email from the system mailbox (`SendFromSystemAsync`) or from a given sender (`SendAsync`) |
| `SystemNotifications/*` | The notification payloads and DTOs: `SystemMessage` and `ForceLogoutMessage` (both `INotificationMessage` push payloads), `NotificationDto`, `NotificationLookup` (a `Shared` `PageQuery` with recipient and status filters), `NotificationStatus` (`None`, `Read`, `Archived`), `NotificationConstants` |
| `SystemNotifications/NotificationHubOptions` | Binding target for `Notifications:Hub` (`Path`, default `/signalr-hub`) |

**`Notifications`**:

| Area | Role |
|---|---|
| `NotificationsModule` | The module's `AppModule`: registers the notification store, `INotificationsModuleApi`, SMTP mail, and the permission provider |
| `DependencyInjection.AddNotificationsServices` / `AddSmtpMail` | `NotificationDbContext` through `AddConfiguredDbContext` and the seam implementation; `IMailService` over the vendor SMTP (MailKit) sender from the `SmtpMail` section |
| `SignalR/SignalRModule`, `SignalR/SignalREndpoint` | A second `AppModule` registering SignalR, the user-id provider, and `IHubService`; an `AppModuleEndpoint` mapping the hub at the configured path |
| `SignalR/NotificationHubOptionsSetup` | Binds and validates `NotificationHubOptions` at startup (`AddNotificationHubOptions`, `IsValidHubPath`); also used by the WebApi host's authentication — see [Design Notes](#design-notes) |
| `Endpoints/` | Controllers on `VersionedApiController`. `NotificationController` searches notifications (`notification.read`), sends one to a user, and pushes a force-logout (`notification.send`); `UserNotificationController` serves the signed-in user's own notifications — search, get (which marks the entry read), and unread count |
| `IntegrationEvents/NotificationsModuleConsumer` | The module's `AppModuleConsumer`, registering the `UserProvisionedIntegrationEvent` consumer |

Visibility inside `Notifications`: the module and endpoint classes, the controllers, `Notification`, `SignalRHub`, `NotificationHubOptionsSetup`, and `NotificationsModuleConsumer` are `public`; `NotificationDbContext` and its initialiser, the seam implementation, the mediator commands/queries and handlers, `IHubService`/`HubService`, `MailService`, the consumer and its definition, and the permission catalog are `internal`. `InternalsVisibleTo` grants `Notifications.Tests`, `DynamicProxyGenAssembly2` (so the tests can mock the internal `IHubService`), and the `MSSQL` migrator.

## Configuration

| Section | Purpose |
|---|---|
| `Notifications:Hub:Path` | Hub path; defaults to `/signalr-hub`. Validated at startup: an absolute path longer than `/`, no trailing `/`, no whitespace, `?`, or `#`, and no overlap with `/api` in either direction |
| `SmtpMail` | `Host`, `Port`, `UseSsl`, `UserName`, `Password`. Required — startup fails when the section is missing. `UserName` doubles as the system sender address |

The connection string is the framework default (`DefaultConnection`). Section placement and checked-in values: [StarterKit.WebApi § Configuration](WebApi.md#configuration).

## Design Notes

- **Handler-centric use cases**: the use-case logic lives in the mediator handlers under `Features/Notifications/{Commands,Queries}`, with behaviour on the aggregate. Sending creates a `Notification` through `Notification.Create`, saves it, then pushes the message to the recipient through `IHubService` — the push follows the save so a client reacting to it can load the stored entry. Marking read loads the recipient's entry and calls `Notification.MarkAsRead()`, which is idempotent and leaves an archived entry archived; an unknown id, or one addressed to another user, is a no-op. The queries read `NotificationDbContext` directly with a projection (`Extensions/DataMapper`). There is no internal notification service; the infrastructure adapters `IHubService` (SignalR) and `IMailService` (SMTP) stay services.
- **Cross-module seam**: `NotificationsModuleApi` only dispatches `SendNotificationCommand` through the mediator. A call through the seam bypasses the controllers' permission attributes; the calling module authorizes the operation first. The cross-module rule this follows is in [coding-conventions.md § Structural Conventions](../../conventions/coding-conventions.md#structural-conventions).
- **SignalR hub**: `SignalRHub` requires an authenticated user, accepts WebSockets only, and closes a connection when its authentication expires (`CloseOnAuthenticationExpiration`). Connections are addressed by the user-id claim (`CustomIdProvider`). `IHubService` sends a typed payload under its type name as the client method name, or a payload-less signal under `NotificationConstants.ServerNotification`. Clients connect with the Identity hub token; the host routes hub-path requests to its `HubBearer` scheme — see [StarterKit.WebApi § Design Notes](WebApi.md#design-notes).
- **Force logout** is a push only (`ForceLogoutMessage` to the user); no notification is stored.
- **One hub path**: the hub path drives both the hub mapping and the host's authentication-scheme routing, so the module and `StarterKit.WebApi` bind and validate it through the same `NotificationHubOptionsSetup`. An invalid value fails startup.
- **Send endpoint**: `NotificationController`'s send action takes the sender id and name from the request, not from the current user.
- **Welcome mail**: `UserProvisionedConsumer` (an `AppConsumer<UserProvisionedIntegrationEvent>` with its `AppConsumerDefinition`, queue prefix `notifications`) sends a welcome mail when Identity provisions a user with an email; the body depends on `ProvisioningSource` (external login vs. otherwise). A send failure is logged and swallowed, so a mail outage does not drive the message through retries to the error queue. No delivery record is kept, so a redelivered event can send the mail twice; that is accepted. The consumer runs only when the event bus is enabled — see [EventBusMassTransitRabbitMQ § Configuration](EventBusMassTransitRabbitMQ.md#configuration).
- **Persistence**: `NotificationDbContext` derives from `BaseDbContext`, uses the schema `notifications` and `DbConnectionNames.Default`, and calls `AuditEntries` on save. The module raises no domain or integration events, so its save path has no dispatch step.
- **Schema creation**: the module's migrations live in the migration projects; `NotificationContextInitialiser` migrates and seeds nothing. Only the MSSQL migrator carries them — see [migrations.md § Migration sets](../../conventions/migrations.md#migration-sets).

## Dependencies

| Depends on | Type (project/package) | Why |
|---|---|---|
| `Notifications.Contracts` | project | The module's own seam, payloads, and hub options |
| `Identity.Contracts` | project | `UserProvisionedIntegrationEvent` and `ProvisioningSource` for the welcome mail |
| `Shared` (via `Notifications.Contracts`) | project | Kernel types, `ICurrentUser`/`IDateTime`, `PageQuery`, permission authorization |
| `Infrastructure` | project | `AppModule`, `AppModuleEndpoint`, controller bases |
| `Persistence` | project | `BaseDbContext`, configured DbContext registration, audit extension, paging, `MigrateDatabaseAsync` |
| `EventBusMassTransitRabbitMQ` | project | Consumer, consumer-definition, and module-consumer bases |
| `Lightsoft.SmtpMail` | package | SMTP sender behind `IMailService` |

Package versions: `Directory.Packages.props`. Full reference graph: [dependency-graph.md](../dependency-graph.md).

## Depended On By

| Project | References |
|---|---|
| `StarterKit.WebApi` | `Notifications` — `NotificationsModule` in the assembly scan list, `NotificationHubOptionsSetup` for scheme routing |
| `src/Migrations/MSSQL` | `Notifications` |
| `tests/Notifications.Tests` | `Notifications`, `Shared` |

No other module references `Notifications.Contracts`.

## Notable Conventions

- The module follows the handler-centric rule; `Identity`'s service-forwarding handlers are the documented deviation — see [coding-conventions.md § Deviations](../../conventions/coding-conventions.md#deviations-from-norms-elsewhere-in-the-repo).
- The module has two `AppModule`s (`NotificationsModule`, `SignalRModule`), both discovered by the host's assembly scan.
- The permission names (`notification.read`, `notification.send`) are `internal` to the module, as Identity's are.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
