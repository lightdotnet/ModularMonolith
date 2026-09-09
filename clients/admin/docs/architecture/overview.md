# Client App Overview: admin

Internal admin console for the ModularMonolith starter template — the first and only client app
(`clients/` has no other subfolder). Layering, dependency direction, and design patterns are in
[architecture.md](./architecture.md); this file covers what the app *does* — functional areas,
routes, backend contract surface, and the auth flow.

## Functional Areas

- **Identity administration** (`/identity/users`, `/identity/roles`) — full CRUD against `Identity.Api`:
  users (with an Active Directory lookup on create, force-password-reset), roles (a permissions
  checklist plus a free-form "other claims" editor).
- **Notifications** (`/notifications` + a topbar bell + a Home inbox) — a permission-gated admin
  send/browse page, plus a live SignalR-backed unread-count/tab-filtered feed shared between the bell
  and the Home page.
- **Organization administration** (`/organization/{companies,departments,employees}`) against
  `Organization.Api` — company CRUD; a company-scoped department/team hierarchy (`OrgUnit`, unified
  via a `Type` discriminator) as a recursive tree with add/edit/move/delete + a read-only "View
  managers" dialog, plus a company-scoped Employee Levels panel; employee CRUD with a tabbed edit
  dialog (Details / Departments & Teams / Login) covering membership assignment (level, primary,
  `Current`/`Acting` status, manager flag) and creating or linking an Identity login.
- **Approvals** (`/approvals`) against `Approval.Api` — a generic multi-level approval workflow: the
  caller's pending decisions and own requests, plus (for `approval.requests.view_all`) an admin
  view-all and a "Create test request" harness that builds an arbitrary-length approver chain.
- **Leave requests** (`/leave-requests`, `/leave-requests/[id]`) against `LeaveManagement.Api` —
  self-service submission/tracking of the caller's own requests (no permission gate, only a session);
  create/edit/delete restricted to the viewer's own requests in an editable status, each requiring a
  real department approver picked from a `GET leave_request/approvers` fetch. A caller with
  `leave.requests.manage` also gets an "All requests" tab (delete-only over every employee's
  requests). The detail page links out to `/approvals/requests/{id}` — decisions happen there, not
  here.
- **Home** (`/`) — a `ProfileSummaryCard` plus the notification inbox; a real Server Component
  resolving the session and fetching the caller's notifications.
- **Auth/session** — an encrypted, proactively-refreshed cookie session (see Auth Flow).
- **Deploy resilience** — both `error.tsx` boundaries recognize deploy-induced stale-tab errors and
  show a health-probe-gated auto-reload notice instead of the generic error card.

## Structure

- **Router**: App Router, rooted at `src/app/`. No `pages/`.
- **Package manager**: pnpm. `pnpm-workspace.yaml` only configures build-script approval — a single
  independent app, not a workspace.
- **Module layout**: `src/modules/<domain>/<name>/` for everything except `src/features/home/` (the
  one holdout). Each folder: `api/` + `components/` + optional `types/`/`constants/`/`hooks/` + an
  `index.ts` barrel. See [architecture.md § Layering](./architecture.md#layering).
- **Data fetching**: server-only, hand-written per feature. Each `api/` has one consolidated
  `<name>.api.ts` wrapping every backend call, normalized through `lib/server/call-guard.ts`;
  `*-action.ts` Server Actions are one file per action (including read-only ones a Client Component
  needs). Whole-list reads happen in async Server Components; writes via a Server Action.
  `modules/notifications` is the one exception — a browser-direct SignalR WebSocket authenticated
  with a short-lived, hub-scoped token. See [architecture.md § Key Design Patterns](./architecture.md#key-design-patterns)
  for the API-layer, DataTable-consumption, and lazy-fetch patterns.
- **State management**: local component state + React Context, no global store. `*-data-table.tsx`
  components drive search/pagination through URL `searchParams`; dialogs use `useActionState` + a
  bumped remount `key`; the Approvals tabs and Leave requests tables render a server-fetched array
  with no owned pagination state. Persisted UI slices (sidebar, accent, theme) each get a Context
  provider; `NotificationsProvider` wraps live data + one shared SignalR connection.
- **Styling**: Tailwind CSS v4, CSS-first config in `src/app/globals.css`. No `tailwind.config.*`.

## Key Routes/Areas

| Route | Path | Notes |
|---|---|---|
| Home | `/` | Async Server Component — resolves the session (redirect to `/login` if absent), renders `ProfileSummaryCard` + `NotificationInbox` (initial page fetched server-side) |
| Profile | `/user-profile` | Account details, QR of the user id, roles/claims, session lifecycle card |
| Login | `/login` | Outside `(dashboard)` — no `AppShell`/session resolution |
| Dashboard layout | `src/app/(dashboard)/layout.tsx` | `resolveSession()` → `SessionGate` wrapping `AppShell`. Sibling `error.tsx` (deploy-recovery branch) + `loading.tsx` (spinner) cascade to nested routes |
| Root layout | `src/app/layout.tsx` | Fonts, `ThemeProvider` → `AccentColorProvider` → `TooltipProvider`, `<AppToaster />`; owns `app/error.tsx` |
| Health probe | `/api/health` | `GET` → `204`, `force-dynamic`, no auth; polled by the deploy-recovery loop |
| Users | `/identity/users` | Gated `identity.users.*`. List/create/edit/delete/force-password |
| Roles | `/identity/roles` | Gated. Client-filtered list; create (name/description only); edit adds a permissions checklist + "Other claims" editor |
| Notifications | `/notifications` | Gated `notification.read`; "Send" gated `notification.send`. Status + recipient filter |
| Companies | `/organization/companies` | Gated. CRUD; edit works off row data (no on-open detail fetch) |
| Departments & Teams | `/organization/departments` | Gated. `?companyId=` picker + recursive tree + "Employee Levels" tab |
| Employees | `/organization/employees` | Gated. Search/paginate; tabbed edit dialog (Details / Departments & Teams / Login) |
| Approvals | `/approvals` | Gated `approval.requests.view`; view-all panel + "Create test request" gated `approval.requests.view_all` |
| Leave requests | `/leave-requests`, `/leave-requests/[id]` | **No permission gate** — any session. `leave.requests.manage` unlocks an "All requests" tab + delete-any |

Every `page.tsx` is a one-line re-export from a feature/module barrel. `constants/nav-items.ts`
assembles `NAV_ITEMS` from each feature's own `NavItem`: `[home, Administration group, Organization
group, /approvals, /leave-requests, Settings]`. `/administration`, `/organization`, `/settings` have
no `page.tsx` and 404 if followed; being ungated they still show in the sidebar and ⌘K palette.

## Backend Integration

Real, but partial. `lib/server/api-clients.ts` registers five backend clients — `Identity`,
`Notifications`, `Organization`, `Approval`, `LeaveManagement` — each resolving its own
`*_API_BASE_URL` env var (the base URL owns its full path prefix; `http.ts` prepends nothing).
`lib/server/backend-api.ts`'s `createBackendApiClient(client)` factory produces five ready instances
(`identityApi` … `leaveManagementApi`); auth is attached by a request-handler pipeline
(`bearerTokenHandler` reads the ambient session), not a passed token. The five backends are logically
separate modules currently co-hosted in one process (`StarterKit.WebApi`).
Error handling, the envelope contract, and the permanent-vs-transient refresh-failure distinction are
covered in [architecture.md § Key Design Patterns](./architecture.md#key-design-patterns).

Endpoints this client consumes, by module:

- **auth** — `auth/token/get`, `auth/token/refresh` (`modules/identity/auth/api/token.api.ts`,
  explicit `client: Identity`); `auth/token/hub` (POST, authenticated — mints the short-lived
  hub-scoped token for the SignalR handshake, `modules/notifications/api/signalr.api.ts`).
- **user-profile** — `user_profile` (GET), `user_profile/token/{list,revoke}`.
- **users** — `user/search`, `user` (GET-all / PUT / DELETE), get-by-id, create, force-password,
  `user/get_domain_user/{userName}` (AD lookup). `user/search` also backs the three on-demand
  user-search components.
- **roles** — `role` (GET-all / POST / PUT / DELETE), get-by-id.
- **permissions** — `permissions` (the definable-permission catalog for the Roles edit dialog).
- **notifications** — `notification` (admin GET/POST), `user_notification` (self-scoped
  GET/mark-read/count), plus a browser-direct WebSocket to `/signalr-hub` (`SIGNALR_HUB_URL`).
- **companies** — `company` (paged search / POST / PUT / DELETE), get-by-id.
- **departments** — `org_unit/company/{id}/tree`, `org_unit/{id}` (GET/PUT), `org_unit/{id}/move`,
  `org_unit` (POST), `org_unit/{id}` (DELETE), `org_unit/{id}/{employee,manager}`; `employee_level/company/{id}`
  + create/update/delete.
- **employees** — `employee/search`, `employee/{id}` (GET/PUT/DELETE), `employee` (POST),
  `employee/{id}/org_unit` (POST) + `/{orgUnitId}` (PUT/DELETE), `employee/{id}/login`
  (POST/PUT/DELETE). `searchEmployees` also resolves employee names for the Leave requests "All
  requests" tab.
- **approvals** — `modules/approvals/api/approvals.api.ts` (admin, `approval.requests.view_all`):
  `approval` (GET search / POST test request). `user-approvals.api.ts` (self-service, server-scoped
  by `UserApprovalController`): `approval/user` (GET / POST), `approval/user/{id}`,
  `approval/user/{id}/decide`.
- **leave-requests** — `leave_request/search`, `leave_request/{id}` (GET/PUT/DELETE),
  `leave_request/approvers`, `leave_request` (POST). `employeeId` search filter is honored
  server-side only for `leave.requests.manage`.

Every function returns a normalized `Result`/`ApiResponse` envelope via `call-guard.ts`. Gated pages
use `lib/server/require-permission.tsx`; `/leave-requests` deliberately does not (see architecture.md
§ Module/Route Boundaries). Permission-string constants live per-feature/module in
`constants/permissions.ts`, matching each backend module's own format (e.g.
`organization.companies.view`, `approval.requests.view_all`, `leave.requests.manage`).

## Auth Flow

Cookie-based session, AES-256-GCM encrypted at rest (`TOKEN_ENCRYPTION_KEY`), with proactive refresh:

1. **Login** — `LoginForm` submits to `loginAction`, which calls `getToken()` then `getCurrentUser()`
   (a profile failure doesn't block login). **Permissions and roles are decoded from the access-token
   JWT** (`lib/server/jwt.ts`), never trusted from the profile API; `claims` is the deduped union of
   both.
2. **Persist** — `persistSessionCookie()` reduces `SessionData` to the minimal `StoredSession`
   (tokens, expiries, profile, `refreshFailureCount`, `extraClaims` — `claims`/`permissions`/`roles`
   dropped, re-derived on read), encrypts it, and writes `admin_session` (`httpOnly`, `sameSite: lax`,
   `maxAge` from a hard 7-day `sessionExpiresAt`). Past `MAX_CHUNK_BYTES` it splits across numbered
   chunk cookies (`cookie-codec.ts`).
3. **Redirect** — `loginAction` honors a safe same-site `?redirect=` path (open-redirect guarded),
   else `/`.
4. **`src/proxy.ts`** — a thin auth gate only: decrypt/validate/hydrate the cookie(s), enforce the
   7-day cap (missing/expired ⇒ `/login?redirect=<path>`, clearing every chunk name), and redirect
   away from `/login` when already authenticated. No token refresh or profile refetch anymore.
5. **`SessionGate`** (`components/layout/session-gate.tsx`, wrapping `AppShell`) drives freshness. On
   a hard navigation it calls `ensureFreshSessionAction({ refetchProfile: true })` behind a full-page
   overlay. That calls `refreshSessionIfNearExpiry()` (`REFRESH_LEAD_MS` = 5 min) which returns a
   `RefreshOutcome` (`skipped` / `success` / `failed{permanent}`); the action maps it to
   `fresh`/`updated`/`retry`/`degraded`. `retry` (transient) retries 2× then polls a
   `SessionUnreachableOverlay` every 5s; `updated` triggers `router.refresh()`; `degraded`
   (permanent 401/400) increments `refreshFailureCount` and force-logs-out at `MAX_REFRESH_FAILURES`
   (3). After settling, a silent 60s interval keeps a long-open session fresh.
6. **Logout** — `logoutAction` deletes every session cookie and redirects to `/login?redirect=<path>`
   (same guard). Session expiry and explicit logout both funnel through the same redirect pattern.
7. **`getSession()`** reads and decrypts the cookie (no fetch); `resolveSession()` is a thin
   passthrough used by the dashboard layout and every gated page.

**SignalR handshake token.** `getSignalRTokenAction()` (`modules/notifications/api/get-signalr-token-action.ts`)
first calls `refreshSessionIfNearExpiry()` so the session bearer is valid (the mint call is
authenticated and `proxy.ts` skips `/api` paths), then calls `getHubToken()` →
`POST auth/token/hub` for a **dedicated hub-audience-scoped token** (~120s, `uid`+`jti` only) — not
the session access token. It returns that token plus the server-resolved `SIGNALR_HUB_URL`.
`use-notifications.ts` passes an `accessTokenFactory` that re-invokes the action on every (re)connect,
so `withAutomaticReconnect()` always gets a fresh short-lived token. This is the one place a token is
readable by browser JS, and it is now a purpose-built token that is rejected on `/api`. `token-cipher.ts`
uses Node's `crypto` and `proxy.ts` has no explicit runtime pin — see
[architecture.md § Known Risks](./architecture.md#known-architectural-risks--debt).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-09_
