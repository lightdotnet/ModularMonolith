# Module Overview: Identity

## Purpose

Owns users, roles, claims, authentication, and session management. Covers three sign-in paths:

- **API token issuance** (`Identity.Api`) — password / Active Directory login, refresh, and session listing/revocation, issuing the session JWT the JSON API and its clients use.
- **Interactive server-rendered login** (`Identity.Web`) — an encrypted-cookie Razor Pages login for browser sessions, plus Microsoft Entra ID external login over OIDC, also relayed to separate-origin clients via a one-time PKCE code exchange (`Pages/Account/ExternalLoginStart.cshtml.cs`, `Pages/Account/ExternalLoginRelay.cshtml.cs`) — see § External Login (OIDC).
- **SignalR hub handshake token** — a dedicated short-lived, hub-audience-only token minted for the notification hub WebSocket so the full session JWT never leaves the httpOnly cookie.

Also owns user CRUD and search, role/claim assignment, and a permission catalog (`PermissionsController` → `GET permissions`) that other modules' permission providers plug into via the same vendor contract.

## Internal Layering

Identity is a **three-project module** (a `.Contracts` seam plus two host-side projects). The template's 4-row Domain/Application/Infrastructure/Api table doesn't apply — adapted to the module's actual projects:

| Project | Responsibility | Notes |
|---|---|---|
| `Identity.Contracts` | DTOs (`UserDto`, `RoleDto`, `TokenDto`, `HubTokenResponse`, `ExchangeAuthCodeRequest`, etc.), request types (`CreateUserRequest`, `SearchUserRequest` — a `record : SearchQuery`), the `AuthProvider` enum (`Local`/`ActiveDirectory`/`EntraId`), service interfaces (`IUserService`, `IRoleService`, `IExternalLoginService`, `IServiceClaimService` — unimplemented), the external-login contract (`ExternalLogin/` — `ExternalLoginDescriptor`, `ExternalLoginOutcome`, `ExternalLoginOptions`), the cross-module integration events (`Users/UserCreatedIntegrationEvent`, `Users/ExternalUserProvisionedIntegrationEvent` — both `INotification`), and the permission catalog (`Authorization/IdentityPermissions` + `IdentityPermissionProvider`). Declares `Lightsoft.Mediator` and `Lightsoft.Result` directly; references `Shared` (so not a leaf). |
| `Identity.Api` | Single project organised by folder: `Entities/` (`User`, `Role`, `RoleClaim`, `UserClaim`, `UserLogin`, `UserRole`, `UserToken`, `UserSession`), `Data/` (`IdentityDbContext`), `Application/Users/{Commands,Queries}`, `Application/Roles/Commands`, `Services/` (`UserService`, `RoleService`, `ExternalLoginService`), `Jwt/` (`AuthenticationService`, `UserSessionService`, `JwtTokenIssuer`, `HubTokenIssuer`, `JwtSigningService` — all `internal`; `JwtServiceCollectionExtensions.AddJwtTokenServices` registers the token services only, no auth scheme), `ExternalLogin/` (`IExternalLoginAuthCodeStore` + `ExternalLoginAuthCodeStore` — the one-time PKCE code store backing the client relay, see § External Login (OIDC)), `Extensions/` (`AuthProviderWire`, `DataMapper`), `Controllers/` (`UserController`, `RoleController`, `TokenController`, `UserProfileController`, `PermissionsController`). | `.Api` suffix kept deliberately — anticipated candidate for future extraction into an independent identity service. |
| `Identity.Web` | Razor Pages host for interactive login. `Pages/Account/{Login,Logout,ExternalLogin,ExternalLoginStart,ExternalLoginRelay}.cshtml(.cs)`, `Pages/Account/ExternalClaimsMapper.cs` (raw OIDC claims → `ExternalLoginDescriptor`), `ExternalLoginRelayOptions.cs` (config section `ExternalLoginRelay` — see § External Login (OIDC)), `DependencyInjection.cs` (`AddIdentityWeb` — cookie + external schemes, `SignInManager<User>`, Razor Pages; `AddMicrosoftOpenIdConnect` gated on `MicrosoftOidcOptions.IsEnabled`), `IdentityWebHost.cs` (`AddIdentityWebHost` — standalone-host composition), `MicrosoftOidcOptions.cs`, `Program.cs` (standalone cookie-only host). References `Identity.Api` directly — an **intra-module** reference inside the Identity bounded context, not a boundary violation. Runs two ways: co-hosted inside `StarterKit.WebApi`, and as its own login-only standalone process. |

## Public Contract

- `UserController` — `GET user` (unbounded list) **and** `GET user/search` (paginated; binds `SearchUserRequest`, dispatches `SearchUserQuery`) coexist for the same read use case ([known-debt.md](../../known-debt.md) D4). Writes (`POST`/`PUT`/`DELETE user`, `PUT user/{id}/password/force`) bind the `Contracts` DTO and dispatch a mediator command.
- `RoleController` — `POST`/`PUT`/`DELETE` dispatch `CreateRole`/`UpdateRole`/`DeleteRole`; reads call `IRoleService` directly.
- `TokenController` — routed at `api/v{version:apiVersion}/auth` (not a `token`-prefixed route). `auth/token/get` + `auth/token/refresh` are `[AllowAnonymous]`; `auth/token/external` (`[AllowAnonymous]`, rate-limited) exchanges a one-time PKCE code for the same `TokenDto` shape via `IExternalLoginAuthCodeStore.ConsumeAsync` — see § External Login (OIDC); `auth/token/hub` (`[Authorize]`) mints the SignalR handshake token via `IAuthenticationService.IssueHubTokenAsync` → returns `HubTokenResponse { AccessToken, ExpiresIn }`.
- `UserProfileController` — route override `api/v{version:apiVersion}/user_profile`. Scopes every action to the caller via `ICurrentUser.UserId`: `GET` (own profile + token-validity check), `GET token/list` (own sessions), `PUT token/revoke`.
- `PermissionsController` — `GET permissions`, returns `IPermissionDefinitionProvider.Define()`.
- **Razor Pages (`Identity.Web`)** — `/Account/Login` (password form + optional "Sign in with Microsoft" button), `/Account/Logout`, `/Account/ExternalLogin` (OIDC callback landing → `IExternalLoginService.ResolveAsync` → cookie sign-in or redirect back to login with an error), `/Account/ExternalLoginStart` + `/Account/ExternalLoginRelay` (the PKCE relay for separate-origin clients — never sets a cookie session, see § External Login (OIDC)). These are server-rendered HTML, not part of the JSON API surface.

## Authentication Wiring

Auth is composed in **two layers** so the same module serves an API host and an interactive-login host:

1. **Module-owned token services** — `Identity.Api`'s `AddJwtTokenServices` binds `JwtOptions` and registers `JwtSigningService` / `JwtTokenIssuer` / `HubTokenIssuer` / `IUserSessionService` / `IAuthenticationService`. It registers **no** authentication scheme and does not touch app-global default schemes.
2. **Web/OIDC schemes** — `Identity.Web`'s `AddIdentityWeb` registers the application-cookie scheme (`IdentityConstants.ApplicationScheme`, also the default sign-in scheme), the external-cookie scheme, `SignInManager<User>`, and — only when `Authentication:Microsoft` is fully configured — the `"Microsoft"` OpenID Connect scheme.
3. **Co-host API composition** — `StarterKit.WebApi/Authentication/ApiAuthenticationExtensions.AddApiAuthentication` (co-host only) adds the JWT `"Bearer"` scheme (via vendor `AddJwtAuth`), the `"Identity.CookieOrBearer"` policy scheme (`ForwardDefaultSelector` routes by **request path**: under the configured hub path → `"HubBearer"`; under `/api` → `"Bearer"`; everything else → the application cookie — the `/api` → `"Bearer"` route keeps API 401/403 flowing through the vendor `OnChallenge`/`OnForbidden` → `UnauthorizedException`/`ForbiddenException` → the Light exception envelope rather than falling to the cookie scheme and returning a bodyless 401), the fail-closed `"HubBearer"` sub-scheme (`ValidateAudience`, `ValidAudience = Jwt:HubAudience`, with its own `OnChallenge` raising `UnauthorizedException` so hub-handshake auth failures return the same envelope), a `PostConfigure` on `"Bearer"` that rejects any token carrying the hub audience and flows token expiry into `Properties.ExpiresUtc`, and the `HubTokenApiGuardHandler` backstop.

The **standalone `Identity.Web` host** (`Identity.Web/Program.cs` → `AddIdentityWebHost`) runs cookie-only: no Bearer scheme, no policy scheme, no `/api` or hub handling. It composes the Identity assembly's platform + mediator services itself (logging + validation behaviours matching the co-host) but scans only the Identity assembly, so it has no cross-module integration-event handlers ([known-debt.md](../../known-debt.md) D6). Because it never calls `AddJwtTokenServices`, the PKCE client relay below is functional only under the co-host.

## External Login (OIDC)

`IExternalLoginService` (`Identity.Contracts/ExternalLogin/`) is the seam; `ExternalLoginService` (`Identity.Api/Services/`, `internal`) implements it. `ResolveAsync(ExternalLoginDescriptor)` returns an `ExternalLoginOutcome` (`Status` ∈ `Linked` / `Provisioned` / `Rejected`, with an `ExternalLoginRejectionReason` when rejected). Rules:

- **Match on provider identity only** — `UserManager.FindByLoginAsync(provider, "{tid}|{oid}")`, never by email.
- **No auto-link** — an external identity whose email is already registered to a local user is **rejected** (`EmailAlreadyRegistered`), never silently linked (the nOAuth attack class). There is no self-service "link my Microsoft account" flow.
- **JIT provisioning** — only when the tenant id is in `ExternalLoginOptions.AllowedTenantIds` *and* the email domain is in `AllowedEmailDomains`. The provisioned user is created via `User.ProvisionFromExternalIdentity` + `UserManager.CreateAsync` (**bypassing `CreateUserCommand`**): passwordless, no roles, `EmailConfirmed = true`, `AuthProvider = EntraId`, active. On success it publishes `ExternalUserProvisionedIntegrationEvent`.
- **Active check** — a linked user that is inactive or soft-deleted is rejected (`UserInactive`).

Configuration lives under `Authentication:Microsoft`, bound twice: `MicrosoftOidcOptions` (in `Identity.Web`, carries `ClientId`/`ClientSecret`/`Instance`/`CallbackPath`/`AllowedTenantIds`; secrets via user-secrets on `StarterKit.WebApi` / `Identity.Web`) drives the OIDC scheme, and `ExternalLoginOptions` (in `Identity.Contracts`, `AllowedTenantIds` + `AllowedEmailDomains`) drives `ExternalLoginService`. Tenant isolation is enforced on the OIDC scheme by a custom `IssuerValidator` (the token's `tid` must be allow-listed **and** the issuer must equal `{instance}{tid}/v2.0`) plus an `OnTokenValidated` re-check.

**Client relay (PKCE code exchange).** `ExternalLoginStart`/`ExternalLoginRelay` (`Identity.Web/Pages/Account/`) reuse the same `ResolveAsync` path above to serve a separate-origin client without ever holding an `Identity.Web` cookie session: on a `Linked`/`Provisioned` outcome they mint a token via the new `IAuthenticationService.IssueTokenForUserAsync` and stage it behind a one-time, PKCE-bound code (`Identity.Api/ExternalLogin/IExternalLoginAuthCodeStore.cs`, backed by `Lightsoft.Caching`'s `ICacheService`), redeemed via `POST auth/token/external`. `ExternalLoginRelayOptions` (section `ExternalLoginRelay`) configures the code TTL and an exact-match `AllowedRedirectUris` allow-list (open-redirect defense) — see the code for exact mechanics/defaults. Both new Razor Pages and the exchange endpoint carry the `"external-login"` rate-limiting policy and are excluded from `RequestLogging`.

## Data Access

`IdentityDbContext` (`Identity.Api/Data/IdentityDbContext.cs`) extends ASP.NET Identity's `IdentityDbContext<...>` directly — can't also extend `Persistence/BaseDbContext` (single inheritance), so it re-applies the Sqlite `DateTimeOffset` fix manually. Configured via `Persistence.DbContextExtensions.AddConfiguredDbContext` (`DbConnectionNames.Identity`, which aliases `Default`). `User` has an index on `Created`, `UserSessions` on `UserId`. Soft-delete is passed as `enableSoftDelete: false` despite `User : ISoftDelete` ([known-debt.md](../../known-debt.md) D2).

`User.AuthProvider` is the `AuthProvider` enum, non-nullable, mapped by EF to a non-nullable `int` column. `AuthProviderWire` (`Identity.Api/Extensions/`) bridges the enum to the legacy wire strings on `UserDto` / `CreateUserRequest` (`Local ↔ null`, `ActiveDirectory ↔ "AD"`, `EntraId ↔ "Microsoft"`), so those DTOs stay `string?` for the admin client — an interim, [known-debt.md](../../known-debt.md).

MSSQL has a single regenerated baseline migration (`src/Migrations/MSSQL/Identity/20260908114120_CreateIdentitySchema.cs`); Sqlite/PostgreSQL have their earlier baselines. Baseline squashing is MSSQL-only while the module is still changing.

**JWT claim assembly** (`Jwt/JwtTokenIssuer.GetUserClaimsAsync`) builds the issued session token's claims from `UserId`/`UserName`, role-derived permission claims (`RoleClaims` filtered by the user's role ids), and `UserManager.GetClaimsAsync(user)` (per-user claims such as `employee_id`, stamped by other modules via `IUserService.SetClaimAsync`) merged in explicitly. `HubTokenIssuer` issues a separate token carrying only `uid` + `jti` and the hub audience — no role/permission/profile claims.

## Dependencies

| Depends on | Type | Why |
|---|---|---|
| `Shared` | project (`Identity.Contracts → Shared`) | `SearchQuery`/`PageQuery` base types; also the transitive route to `Lightsoft.AspNetCore.Authorization` used by `IdentityPermissionProvider` (undeclared transitive — [known-debt.md](../../known-debt.md)). |
| `Infrastructure` | project (`Identity.Api → Infrastructure`, `Identity.Web → Infrastructure`) | `VersionedApiController`, `AppModule`/`AppModuleEndpoint`, shared infrastructure composition, `AddAppCache` (see `Lightsoft.Caching` row below). |
| `Persistence` | project (`Identity.Api → Persistence`) | `AddConfiguredDbContext`, audit/paging extensions. |
| `Identity.Api` | project (`Identity.Web → Identity.Api`) | Intra-module — `Identity.Web` reuses the `User` entity, `IdentityDbContext`, `AddIdentityServices`, and `IExternalLoginService`. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Identity.Core` | package | `IdentityUser`/`IdentityRole`/`UserManager`/`SignInManager` base types. |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | package (`Identity.Web`) | The Microsoft Entra ID OIDC scheme. |
| Vendor `Lightsoft.ActiveDirectory` | package | AD password check in `AuthenticationService` (Windows only; a fake service otherwise). |
| Vendor `Lightsoft.Caching` | package (`Identity.Api`) | `ICacheService`, backing the one-time external-login code store (`ExternalLoginAuthCodeStore`) — see § External Login (OIDC). |
| Vendor `Lightsoft.SharedKernel` | package | `Light.Domain` base types for entities. |
| Vendor `Lightsoft.Mediator` / `Lightsoft.Result` | package (`Identity.Contracts`) | `INotification` on the integration events; `Result`/`Result<T>` return types. Declared directly. |

`Identity.Api` **no longer references `Notifications.Contracts`** — the welcome-email side effect moved to `Notifications.Api` (see below).

## Depended On By

- `StarterKit.WebApi` (composition-root host) references `Identity.Api` and `Identity.Web`.
- `Notifications.Api` references `Identity.Contracts` — its `Application/Users/EventHandlers/` handle `UserCreatedIntegrationEvent` and `ExternalUserProvisionedIntegrationEvent` (welcome / SSO-welcome mail). This is the only cross-module edge between Identity and Notifications, and it now runs **Notifications → Identity.Contracts** (the reverse of the old direction). Compliant — only the `Contracts` seam is reached.
- `Organization.Api` references `Identity.Contracts` — `IUserService` (create/link an Identity login for an employee) and `IUserService.SetClaimAsync` (stamp/clear the `employee_id` claim).
- `Identity.Tests` — `Identity.Api.csproj` grants `InternalsVisibleTo` so the `internal` command/query records and JWT classes can be unit-tested directly.
- `StarterKit.WebApi/Authentication/` composes Identity's token services into the API auth pipeline (co-host only).

Client-side integration is not re-inspected in backend-only syncs — see `clients/admin/docs/`.

## Notable Conventions

- **CQRS is the entrypoint for every write + user search, but handlers delegate to the service classes.** Controllers bind the `Contracts` DTO and dispatch an `internal` mediator command/query; the handler forwards to `IUserService`/`IRoleService` (exception: `SearchUserQueryHandler` queries `UserManager<User>` directly). Tracked as [known-debt.md](../../known-debt.md) D1.
- **Cross-module reactions use `Contracts`-level integration events (`INotification`), published manually via `IPublisher`.** Identity has **two** publish sites: `CreateUserCommandHandler` (`UserCreatedIntegrationEvent`, after a successful create) and `ExternalLoginService` (`ExternalUserProvisionedIntegrationEvent`, after JIT provisioning). Identity defines no `BaseEntity` domain events and never calls the `Persistence` `DispatchDomainEvents` convention — [known-debt.md](../../known-debt.md) P6.
- **Auth composition is host-owned, not module-owned.** The module registers token services and (via `Identity.Web`) the cookie/OIDC schemes; the co-host composes the Bearer + policy + hub schemes in `AddApiAuthentication`. See § Authentication Wiring.
- **Hub-token audience isolation is fail-closed.** The `"HubBearer"` sub-scheme rejects any token whose `aud` is not `Jwt:HubAudience`; `HubTokenApiGuardHandler` is a fail-open backstop keeping a hub token off `/api`. The hub token also carries no role/permission claims, so `[MustHavePermission]` endpoints reject it regardless. The hub path that drives this scheme routing is bound from configuration (`Notifications:Hub:Path`, default `/signalr-hub`) and validated fail-closed at startup so it cannot overlap the `/api` namespace.
- `ClaimTypeConstants.TokenId` is `"jti"`, not `"tid"` — `"tid"` collides with `JwtSecurityTokenHandler`'s legacy inbound claim map (and, now, with the OIDC tenant-id claim).
- `IdentityPermissionProvider` (`Identity.Contracts`) uses vendor `Light.AspNetCore.Authorization` types without declaring the package directly — rides in via `Shared` ([known-debt.md](../../known-debt.md)).
- `IdentityDbContext` passes `enableSoftDelete: false` despite `User : ISoftDelete`; `AuthenticationService.CheckInvalidUser` checks `user.Deleted != null` ([known-debt.md](../../known-debt.md) D2).
- `IServiceClaimService` has no registered implementation (commented out in `DependencyInjection.cs`); `IdentityClaimQueryExtensions.CheckUserHasClaimAsync` is dead code ([known-debt.md](../../known-debt.md) P4, P5).
- `Identity.Api.csproj` declares `<InternalsVisibleTo Include="Identity.Tests" />`.
- **`IUserService.SetClaimAsync(userId, claimType, claimValue)`** is a generic opaque per-user claim setter (`null` value removes the claim). Its one caller is `Organization`'s employee-login commands, stamping/clearing `ClaimTypeConstants.EmployeeId` (`"employee_id"`); `CurrentUserBase.EmployeeId` reads it back off the principal. Reaching the token depends on the `GetUserClaimsAsync` merge above.
- **`Login.cshtml.cs` spends equivalent PBKDF2 time on an unknown username** (a fixed dummy hash) to remove a login-timing oracle.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-11_
