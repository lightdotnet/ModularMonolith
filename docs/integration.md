# Integration — Backend ↔ Clients

Cross-cutting facts that span both `src/` and `clients/*` — the integration boundary itself, not owned by either project. See [src/CLAUDE.md](../src/CLAUDE.md) and [clients/admin/CLAUDE.md](../clients/admin/CLAUDE.md) for each side's own architecture.

## Intended Shape

- **Backend** (`src/`): ASP.NET Core, C#, Modular Monolith. Flat projects directly under `src/`, plus shared/building-blocks project(s) and a composition-root host (`StarterKit.WebApi`). The Identity module also ships `Identity.Web`, a Razor Pages login host — server-rendered HTML for interactive/OIDC sign-in, not part of the JSON API a client consumes.
- **Clients** (`clients/`): one or more frontend apps, each in `clients/<app-name>/`. The primary one (`clients/admin/`) is Next.js (App Router), TypeScript/React. Do not assume there's only one client app.
- **Integration**: backend MVC controllers are API-only (JSON, no Razor views); each client app is a consumer of that API, never a UI rendered by the backend. No shared source, no shared DB access, no in-process calls between `src/` and `clients/*`.

## API Contract

| App | Client generation strategy | Base URL / env config | Auth flow |
|---|---|---|---|
| admin | Hand-written, one consolidated `<feature>.api.ts` per feature under `modules/<domain>/<feature>/api/` — no OpenAPI-generated client | Eight named backend clients (`identityApi`/`notificationsApi`/`organizationApi`/`locationApi`/`approvalApi`/`leaveManagementApi`/`catalogApi`/`ordersApi` via `lib/server/`), each with its own server-only base-URL env var (`IDENTITY_API_BASE_URL`/`NOTIFICATIONS_API_BASE_URL`/`ORGANIZATION_API_BASE_URL`/`LOCATION_API_BASE_URL`/`APPROVAL_API_BASE_URL`/`LEAVE_MANAGEMENT_API_BASE_URL`/`CATALOG_API_BASE_URL`/`ORDERS_API_BASE_URL`; the base URL owns its full path/version prefix). Real-time notifications use a **server-only** `SIGNALR_HUB_URL` (not `NEXT_PUBLIC_`-inlined), read server-side and handed to the browser at connect time so it stays a runtime setting | Encrypted httpOnly cookie session (`admin_session`, AES-256-GCM), permissions/roles decoded from the access-token JWT; `src/proxy.ts` enforces the session cap, `components/layout/session-gate.tsx` proactively refreshes a near-expiry token. The SignalR handshake is the one deliberate exception to "the token never leaves the cookie" — see below and [clients/admin/docs/architecture/overview.md § Auth Flow](../clients/admin/docs/architecture/overview.md#auth-flow) |

## Notable cross-cutting facts

- **SignalR hub handshake.** The browser connects **directly to the backend hub** (`SIGNALR_HUB_URL`, absolute — the admin app does not proxy it same-origin), so the backend must allow CORS for the client origin. The handshake is authenticated not with the full session JWT but with a **short-lived (~120s), hub-audience-only token** minted by `POST api/v1/auth/token/hub` (an authenticated Identity endpoint). That token carries only `uid` + `jti` and `aud = "signalr-hub"`; the backend's `"HubBearer"` scheme rejects it on `/api`, and `CloseOnAuthenticationExpiration` drops the socket when it lapses. The admin client re-mints it per (re)connect via a Server Action.
- **Notification deep links.** A `Notification.Url` starting with `/` is an app-relative deep link the admin client renders as a `next/link` (e.g. the Approval module sends `/approvals/requests/{id}`). External/absolute URLs stay plain non-navigating rows.
- **`AuthProvider` on the user contract.** The backend models `User.AuthProvider` as an enum (`Local`/`ActiveDirectory`/`EntraId`) but the `UserDto` / `CreateUserRequest` wire contract still carries it as `string?` (`null` / `"AD"` / `"Microsoft"`), bridged server-side — the admin client sees only the string form.
- **`employee_id` claim.** When an employee is linked to an Identity login, that user carries an `employee_id` claim in the JWT; the Approval module's self-service create reads it to stamp `RequesterEmployeeId` server-side, and LeaveManagement uses it to scope a caller's own requests.
- **Leave requests delegate approval, not the reverse.** The admin client's `/leave-requests` pages call `LeaveManagement.Api` for CRUD only; the multi-level decision workflow runs in `Approval.Api`. The leave-request detail page links out to `/approvals/requests/{approvalRequestId}`.
- **Microsoft login relay (PKCE authorization-code exchange).** A second sign-in path alongside the direct password/AD login: the browser is redirected through `Identity.Web` (a distinct, browser-reachable origin — `IDENTITY_WEB_BASE_URL` on the client, not the server-to-server `IDENTITY_API_BASE_URL`) to complete Microsoft Entra ID OIDC, then handed back to the admin client with a one-time, PKCE-bound code instead of a token. The client exchanges that code server-to-server for the same `TokenDto` shape a normal login returns, converging on the identical session-establishment step either way. See `clients/admin/docs/architecture/overview.md § Auth Flow` and `src/docs/architecture/modules/Identity.md § External Login (OIDC)` for the two sides' mechanics.

---
_Last synced: 2026-09-16_
