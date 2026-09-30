# Client App Overview: admin

Internal admin console for the StarterKit Modular Monolith — the only client app (`clients/` has no
other app subfolder). Layering, dependency direction, and design patterns are in
[architecture.md](./architecture.md); this file covers what the app *does* — functional areas,
routes, backend contract surface, and the auth flow. The backend ↔ client boundary itself is in
[docs/integration.md](../../../../docs/integration.md).

## Functional Areas

- **Identity administration** (`/identity/users`, `/identity/roles`) — full CRUD against the Identity
  module: users (with an Active Directory lookup on create, force-password-reset), roles (a
  permissions checklist plus a free-form "other claims" editor).
- **Notifications** (`/notifications` + a topbar bell + a Home inbox) — a permission-gated admin
  send/browse page, plus a live SignalR-backed unread-count/tab-filtered feed shared between the bell
  and the Home page.
- **Home** (`/`) — a `ProfileSummaryCard` plus the notification inbox; a real Server Component
  resolving the session and fetching the caller's notifications.
- **User profile** (`/user-profile`) — the caller's account, roles/claims/permissions, and session
  lifecycle.
- **Auth/session** — an encrypted, proactively-refreshed cookie session, reached via password login or
  a Microsoft PKCE relay (see Auth Flow).
- **Deploy resilience** — both `error.tsx` boundaries recognize deploy-induced stale-tab errors and
  show a health-probe-gated auto-reload notice instead of the generic error card.

## Structure

- **Router**: App Router, rooted at `src/app/`. No `pages/`.
- **Package manager**: pnpm. `pnpm-workspace.yaml` only configures build-script approval — a single
  independent app, not a workspace.
- **Module layout**: `src/modules/<domain>/<name>/` (or a flat `src/modules/<name>/`) for everything
  except `src/features/home/` (the one holdout). Each folder: `api/` + `components/` + optional
  `types/`/`constants/`/`hooks/` + an `index.ts` barrel. See [architecture.md § Layering](./architecture.md#layering).
- **Data fetching**: server-only, hand-written per feature. Each `api/` has one consolidated
  `<name>.api.ts` wrapping every backend call, normalized through `lib/server/call-guard.ts`;
  `*-action.ts` Server Actions are one file per action (including read-only ones a Client Component
  needs). Whole-list reads happen in async Server Components; writes via a Server Action.
  `modules/notifications` adds the one browser-direct channel — a SignalR WebSocket authenticated
  with a short-lived, hub-scoped token. See [architecture.md § Key Design Patterns](./architecture.md#key-design-patterns).
- **State management**: local component state + React Context, no global store. `*-data-table.tsx`
  components drive search/pagination through URL `searchParams`; dialogs use `useActionState` + a
  bumped remount `key`. Persisted UI slices (sidebar, accent, theme) each get a Context provider;
  `NotificationsProvider` wraps live data + one shared SignalR connection.
- **Styling**: Tailwind CSS v4, CSS-first config in `src/app/globals.css`. No `tailwind.config.*`.

## Key Routes/Areas

| Route | Path | Notes |
|---|---|---|
| Home | `/` | Async Server Component — resolves the session (redirect to `/login` if absent), renders `ProfileSummaryCard` + `NotificationInbox` (initial page fetched server-side) |
| Profile | `/user-profile` | Account details, QR of the user id, roles/claims/permissions, session lifecycle card. Super-admin-only extras (`isSuperAdminUser`): a manual "Refresh now" action and a "Session tokens" card exposing the raw access/refresh token with copy + show/hide |
| Login | `/login` | Outside `(dashboard)` — no `AppShell`/session resolution. Password form + "Continue with Microsoft" |
| Microsoft login relay | `/login/microsoft/start`, `/login/microsoft/callback` | Route Handlers, not pages — see Auth Flow |
| Dashboard layout | `src/app/(dashboard)/layout.tsx` | `resolveSession()` → `SessionGate` wrapping `AppShell`. Sibling `error.tsx` (deploy-recovery branch) + `loading.tsx` (spinner) cascade to nested routes |
| Root layout | `src/app/layout.tsx` | Fonts, `ThemeProvider` → `AccentColorProvider` → `TooltipProvider`, `<AppToaster />`; owns `app/error.tsx` |
| Health probe | `/api/health` | `GET` → `204`, `force-dynamic`, no auth; polled by the deploy-recovery loop |
| Users | `/identity/users` | Gated `identity.users.view`; create/update/delete on `identity.users.{create,update,delete}`. List/create/edit/delete/force-password |
| Roles | `/identity/roles` | Gated `identity.roles.view`; mutations `identity.roles.manage`. Client-filtered list; create (name/description only); edit adds a permissions checklist + "Other claims" editor |
| Notifications | `/notifications` | Gated `notification.read`; "Send" gated `notification.send`. Status + recipient filter |

Page files, nav assembly, and the nav group routes without a `page.tsx` are covered in
[architecture.md § Layering](./architecture.md#layering) and
[§ Module / Route Boundaries](./architecture.md#module--route-boundaries).

## Backend Integration

The client holds one named backend client per backend module — Identity and Notifications — each
with its own `<MODULE>_API_BASE_URL` env var (the base URL owns its full path prefix; `http.ts`
prepends nothing) and a ready instance exported by `lib/server/backend-api.ts`'s
`createBackendApiClient(client)` factory — the full list is in
[development-guide.md § Environment](../conventions/development-guide.md#environment). Auth is
attached by a request-handler pipeline (`bearerTokenHandler` reads the ambient session), not a passed
token. Both modules are co-hosted in one process, `StarterKit.WebApi` — see
[StarterKit.WebApi](../../../../docs/architecture/projects/WebApi.md). Error handling, the envelope
contract, and the permanent-vs-transient refresh-failure distinction are covered in
[architecture.md § Key Design Patterns](./architecture.md#key-design-patterns).

Endpoints this client consumes, by area:

- **auth** — `auth/token/get`, `auth/token/refresh`, `auth/token/external` (POST — exchanges a one-time
  PKCE code for a token; see Auth Flow) (`modules/identity/auth/api/token.api.ts`, explicit
  `client: Identity`); `auth/token/hub` (POST, authenticated — mints the short-lived hub-scoped token
  for the SignalR handshake, `modules/notifications/api/signalr.api.ts`). The Microsoft relay's
  browser-facing leg targets `Identity.Web` directly (`IDENTITY_WEB_BASE_URL`, not this client's
  `Identity` backend client) — see Auth Flow.
- **user-profile** — `user_profile` (GET), `user_profile/token/{list,revoke}`.
- **users** — `user/search`, `user` (GET-all / PUT / DELETE), get-by-id, create, force-password,
  `user/get_domain_user/{userName}` (AD lookup). `user/search` also backs the on-demand user-search
  component in `notifications`.
- **roles** — `role` (GET-all / POST / PUT / DELETE), get-by-id.
- **permissions** — `permissions` (the definable-permission catalog for the Roles edit dialog).
- **notifications** — `notification` (admin GET/POST), `user_notification` (self-scoped
  GET/mark-read/count), plus a browser-direct WebSocket to the hub (`SIGNALR_HUB_URL`).

The backend side of these endpoints is documented in [Identity](../../../../docs/architecture/projects/Identity.md)
and [Notifications](../../../../docs/architecture/projects/Notifications.md). Every function returns
a normalized `Result`/`ApiResponse` envelope via `call-guard.ts`. Gated pages use
`lib/server/require-permission.tsx`. Permission-string constants live per-feature in `constants/`,
matching the backend module's own format (e.g. `identity.users.view`, `notification.send`).

## Auth Flow

Cookie-based session, AES-256-GCM encrypted at rest (`TOKEN_ENCRYPTION_KEY`), with proactive refresh.
Two entry points converge on the same session-establishment step:

1. **Password login** — `LoginForm` submits to `loginAction`, which calls `getToken()` then
   `getCurrentUser()` (a profile failure doesn't block login), then `establishSession()`.
2. **Microsoft login** — `ExternalLoginLink` (a plain server-rendered `<a>`, a real top-level
   navigation) sends the browser to `/login/microsoft/start`, which generates a PKCE verifier/challenge
   pair (`lib/server/external-login-pkce.ts`), stores the verifier in a short-lived HttpOnly cookie, and
   redirects to `Identity.Web`'s `/Account/ExternalLoginStart` (`IDENTITY_WEB_BASE_URL` — a distinct,
   browser-reachable origin from the server-to-server `IDENTITY_API_BASE_URL`). After the backend's own
   relay completes, it redirects back to `/login/microsoft/callback`, which reads+clears the PKCE
   cookie, exchanges the code via `exchangeExternalLoginCode()` (`auth/token/external`), and also calls
   `establishSession()`. See `app/login/microsoft/{start,callback}/route.ts`.
3. **`establishSession()`** (`modules/identity/auth/api/establish-session.ts`, shared by both paths
   above) — **permissions and roles are decoded from the access-token JWT** (`lib/server/jwt.ts`),
   never trusted from the profile API; `claims` is the deduped union of both.
4. **Persist** — `persistSessionCookie()` reduces `SessionData` to the minimal `StoredSession`
   (tokens, expiries, profile, `refreshFailureCount`, `extraClaims` — `claims`/`permissions`/`roles`
   dropped, re-derived on read), encrypts it, and writes `admin_session` (`httpOnly`, `sameSite: lax`,
   `maxAge` from a hard 7-day `sessionExpiresAt`). Past `MAX_CHUNK_BYTES` it splits across numbered
   chunk cookies (`cookie-codec.ts`).
5. **Redirect** — both entry points honor a safe same-site return path (open-redirect guarded — the
   Microsoft path carries it through as `state`), else `/`. On failure, either path redirects to
   `/login?error=...`, rendered by the same `Alert` `LoginForm` already uses.
6. **`src/proxy.ts`** — a thin auth gate only: decrypt/validate/hydrate the cookie(s), enforce the
   7-day cap (missing/expired ⇒ `/login?redirect=<path>`, clearing every chunk name), and redirect
   away from the public auth paths when already authenticated. The public-path check is an explicit
   allow-list (`/login`, `/login/microsoft/start`, `/login/microsoft/callback`), not a single
   comparison. No token refresh or profile refetch.
7. **`SessionGate`** (`components/layout/session-gate.tsx`, wrapping `AppShell`) drives freshness. On
   a hard navigation it calls `ensureFreshSessionAction({ refetchProfile: true })` behind a full-page
   overlay. That calls `refreshSessionIfNearExpiry()` (`REFRESH_LEAD_MS` = 5 min) which returns a
   `RefreshOutcome` (`skipped` / `success` / `failed{permanent}`); the action maps it to
   `fresh`/`updated`/`retry`/`degraded`. `retry` (transient) retries 2× then polls a
   `SessionUnreachableOverlay` every 5s; `updated` triggers `router.refresh()`; `degraded`
   (permanent 401/400) increments `refreshFailureCount` and force-logs-out at `MAX_REFRESH_FAILURES`
   (3). After settling, a silent 60s interval keeps a long-open session fresh.
8. **Logout** — `logoutAction` deletes every session cookie and redirects to `/login?redirect=<path>`
   (same guard). Session expiry and explicit logout both funnel through the same redirect pattern.
9. **`getSession()`** reads and decrypts the cookie (no fetch); `resolveSession()` is a thin
   passthrough used by the dashboard layout and every gated page.

**SignalR handshake token.** `getSignalRTokenAction()` (`modules/notifications/api/get-signalr-token-action.ts`)
first calls `refreshSessionIfNearExpiry()` so the session bearer is valid (the mint call is
authenticated and `proxy.ts` skips `/api` paths), then calls `getHubToken()` →
`POST auth/token/hub` for a **dedicated hub-audience-scoped token** (~120s, `uid`+`jti` only) — not
the session access token. It returns that token plus the server-resolved `SIGNALR_HUB_URL`.
`use-notifications.ts` passes an `accessTokenFactory` that re-invokes the action on every (re)connect,
so `withAutomaticReconnect()` always gets a fresh short-lived token that is rejected on `/api`. The
only other places a token reaches the browser are the short-lived PKCE `code_verifier` cookie (never
an access token) used by the Microsoft login relay above, and the super-admin-only "Session tokens"
card on `/user-profile` (`isSuperAdminUser` gate), which renders the raw session access/refresh token
for inspection. `token-cipher.ts` uses Node's `crypto` and `proxy.ts` has no explicit runtime pin — see
[architecture.md § Known Risks](./architecture.md#known-architectural-risks--debt).

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
