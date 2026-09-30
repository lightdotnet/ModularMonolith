# Architecture: admin

Structure, boundaries, and dependency direction of the admin client. This file stays at the
"stable shape" altitude — per-feature behaviour, the route table, and the endpoint list live in
[overview.md](./overview.md); package references and the full barrel-bypass inventory live in
[dependency-graph.md](./dependency-graph.md).

## Layering

Feature-folder layering, split across two top-level roots under `clients/admin/src/`:

```text
src/
  app/                  routing only — no logic. Each page.tsx is a re-export of a feature/module
                         barrel's page component (or a thin wrapper forwarding route params /
                         searchParams to one). Root + (dashboard) error.tsx boundaries branch on
                         isRecoverableDeploymentError (deploy-stale-tab self-recovery); (dashboard)/
                         loading.tsx is a centered spinner cascading to nested routes; api/health/
                         route.ts is a static 204 liveness probe; login/microsoft/{start,callback}/
                         route.ts are the Microsoft login relay's Route Handlers. Nav-only group
                         routes have no page.tsx (see Module / Route Boundaries).
  features/home/         the one feature not under modules/ — components/, constants/nav-item.ts,
                         index.ts; no api/ of its own (calls other modules' barrels/actions).
  modules/<domain>/<name>/
                         one folder per feature — nested under a domain folder where one backend
                         module spans several features (identity/{auth,user-profile,users,roles}),
                         flat where it doesn't (notifications). Each owns: api/ (one consolidated
                         <name>.api.ts + one file per *-action.ts Server Action), components/,
                         optional types/, constants/ (permissions, nav item), hooks/, context/, and
                         an index.ts barrel — the only sanctioned cross-module import surface.
  components/
    ui/                  shadcn-CLI primitives plus a few hand-written/hand-modified additions. Leaf
                         layer.
    foundation/          low-level listbox / virtual-list / floating-overlay hooks serving
                         components/command/* and the virtualized DataTable body, plus
                         portal-container.ts (React Context letting a Popover portal into an open
                         Dialog's own DOM node).
    command/             Cmd/Ctrl+K palette; reached via components/shared/search-box.tsx.
    layout/              app chrome — topbar, sidebar, shell, breadcrumbs, user menu, the session gate
                         and its overlays, the deploy-recovery notice.
    theme/               theme + accent-color providers/pickers.
    shared/              cross-feature building blocks with no feature knowledge (data-table/, the
                         search box, access-denied, hydration-safe rendering helpers).
    toast/               themed sonner wrapper (notifySuccess / notifyError).
  hooks/                 app-wide client hooks (sidebar/scroll state, guarded-action and
                         action-success-toast helpers).
  lib/
    server/              server-only — every file under lib/server/ starts with import "server-only":
                         the fetch wrapper (http.ts) and its request-handler pipeline, call-guard.ts,
                         config.ts, the backend-client registry (api-clients.ts / backend-api.ts), the
                         session + cookie-codec + JWT + refresh chain, the PKCE helper, and the
                         permission helpers including require-permission.tsx (the page gate, returns
                         JSX).
    shared/              client-safe helpers (cn, menu building, permission logic, display/status
                         helpers); deployment-recovery.ts ("use client") is the browser-only outlier.
  constants/nav-items.ts  assembly only — imports each feature/module's own NavItem (by direct file
                         path, not barrel — see Dependency Direction) and composes NAV_ITEMS.
  types/                 app-wide shared types (API envelope, claims, nav, session).
  proxy.ts               thin auth gate only — decrypt / validate / hydrate the session cookie(s),
                         enforce the hard 7-day session cap, keep /login unreachable once authenticated.
                         No token refresh, no feature/module imports.
```

`components/layout/*` and `components/theme/*` (app chrome) compose `components/ui/*` +
`hooks/*` + `lib/shared/*`; `components/shared/*` and `components/toast/*` are cross-feature,
feature-agnostic building blocks at the same layer; `components/ui/*` is the leaf primitive layer.

**Nav tree assembly**: each nav-bearing feature/module owns its `NavItem` in its `constants/`
(label, href, icon, and — where gated — the permission). `constants/nav-items.ts` only *assembles*
these into `NAV_ITEMS`, declaring itself only the group nodes that have no owning feature (the
"Administration" group over Users, Roles, and Notifications). `NavItem.exact` marks an item active
only on an exact path match, for a parent whose sibling lives under its path. `Sidebar` and the
topbar `SearchBox` both filter this same tree client-side via `lib/shared/menu.ts`'s
`buildVisibleMenu(NAV_ITEMS, can)`.

## Dependency Direction

Verified via actual `import` statements. An arrow means "is imported by", never the reverse.
See [dependency-graph.md](./dependency-graph.md#circular-references) for the full, current
barrel-bypass exception list and rationale.

```text
components/ui/*, components/foundation/*        leaf primitives. Never import layout/theme/* or a
        ^                                        feature/module. One narrow exception: ui/{dialog,
        |                                        popover}.tsx import foundation/portal-container.ts.
components/shared/*, components/toast/*,        feature-agnostic building blocks. Consumed by every
components/command/*                            feature/module + layout/*; import nothing from a
        ^                                        feature/module themselves.
        |
lib/shared/*, lib/server/*                      pure helpers / server-only session+HTTP layer.
        ^                                        Normally below every feature/module; two deliberate
        |                                        reversals — lib/server/refresh-session.ts ->
        |                                        identity/auth/api/token.api.ts, and
        |                                        lib/server/refetch-profile.ts ->
        |                                        identity/user-profile/api/user-profile.api.ts
        |                                        (neither is a cycle — those modules don't import back).
feature/module internals (api, components,     api/ owns backend calls; constants/ owns permission
  constants, types)                            strings + nav metadata.
        ^
a feature/module's index.ts barrel             the only sanctioned cross-module import surface. A
        ^                                        reasoned set of exceptions bypass it via direct file
        |                                        import — to keep a barrel's server-only exports out
        |                                        of a client bundle, or because the target (a Server
        |                                        Action, a component) was never barrel-exported.
        |                                        Full list in dependency-graph.md.
components/layout/*, components/theme/*         app chrome. Reaches into three specific feature files
        ^                                        directly (topbar -> notification-bell, user-menu ->
        |                                        logout-action, session-gate -> ensure-fresh-session-action).
app/**/{page,layout}.tsx, app/api/health,      routing — barrel re-exports or thin params wrappers
src/proxy.ts                                    (see Layering), or (proxy.ts) direct lib/server/* calls
                                                only.
```

No cycles found among internal imports.

## Key Design Patterns

- **Feature/module folder + barrel export.** Every feature/module owns `api/`, `components/`, and
  optionally `types/`/`constants/`, exposed through a single `index.ts` that is the only sanctioned
  cross-module import. A reasoned set of direct-file-import exceptions exists — see
  [dependency-graph.md](./dependency-graph.md#circular-references).

- **One consolidated API file per feature/module; Server Actions kept separate.** Each `api/` has one
  `<name>.api.ts` wrapping every backend call that feature makes (not one file per endpoint); each
  `*-action.ts` Server Action is its own file, including read-only actions a Client Component needs but
  can't fetch itself (e.g. `get-role-detail-action.ts`). `resolve-session.ts` is deliberately its own
  file — cookie-only, no backend call.

- **Server-only API layer with normalized envelopes.** `lib/server/http.ts` (`requestJson`/`requestVoid`)
  is the single fetch wrapper; every `<name>.api.ts` returns a normalized result via `call-guard.ts`'s
  `guardCall`/`guardResponseCall`/`guardRawCall`. A non-2xx throws a typed `HttpError` carrying the
  status, which `call-guard.ts` maps to the backend's `ResultCode` vocabulary — this is what lets the
  refresh flow tell a dead token (401/400) from a network blip. **Every backend response is
  envelope-wrapped** — `guardRawCall` is almost never correct here.

- **Auth-token injection via a request-handler pipeline.** `http.ts` has no `accessToken` option — it
  takes `handlers` run before `fetch`. `lib/server/backend-api.ts`'s `createBackendApiClient(client)`
  factory pre-wires `bearerTokenHandler` (reads the ambient session) and a fixed backend client; it
  exports one instance per backend module (the full list is in
  [development-guide.md § Environment](../conventions/development-guide.md#environment)). Pre-session
  call sites pass `explicitBearerTokenHandler(token)` instead. `http.ts` never imports sessions.

- **Session freshness off blocking middleware, in a client-driven gate.** `proxy.ts` only enforces
  the 7-day cap and the `/login` redirect. `components/layout/session-gate.tsx` (mounted in
  `(dashboard)/layout.tsx`) drives `ensureFreshSessionAction()` — a state machine
  (`checking`/`ready`/`unreachable`) showing a full-page overlay while checking on hard navigation,
  retrying a transient failure up to 2× (then polling every 5s), and force-logging-out after
  `MAX_REFRESH_FAILURES` (3) consecutive *permanent* refresh failures rather than riding out the
  7-day cap. A silent 60s background interval keeps a long-open soft-nav-only session fresh.

- **Near-expiry refresh centralized with a distinguishable failure verdict.**
  `lib/server/refresh-session.ts`'s `refreshSessionIfNearExpiry(session)` returns a `RefreshOutcome`
  union (`skipped` / `success` / `failed{permanent}`) so callers separate "not due" from "attempted
  and failed", and transient (network/5xx) from permanent (401/400 — refresh token itself invalid).
  `ensureFreshSessionAction` and `get-signalr-token-action.ts` both call it; only the former does the
  failure-counting.

- **Minimal encrypted cookie storage, hydrated on every read.** The `admin_session` cookie persists
  only `StoredSession` (tokens, expiries, profile, `refreshFailureCount`, `extraClaims`) —
  `claims`/`permissions`/`roles` are dropped and re-derived from the access-token JWT on every read.
  `cookie-codec.ts` splits the encrypted payload across numbered chunk cookies past `MAX_CHUNK_BYTES`
  as a backstop against the browser's silent ~4096-byte per-cookie limit; every write clears the
  cookie names it isn't using.

- **Permissions/roles decoded from the JWT, never trusted from the profile API.** `lib/server/jwt.ts`
  extracts the `permission`/`role` claim types from the access token (no signature check — issued by
  this app's own backend); `build-session-claims.ts` unions with the profile API's claims for display
  only. Both `loginAction` and `refreshSession()` re-derive on every token issuance.

- **Shared `requirePermission` / `AccessDenied` page gate.** `lib/server/require-permission.tsx`
  resolves the session (redirecting to `/login` if none), checks the permission, and returns
  `{ session, denied }`; a page does `if (denied) return denied;` before its own fetch. Every gated
  page uses this.

- **Secondary permissions branch the UI.** Beyond the route gate, pages call
  `hasPermission(session, …)` (`lib/server/authorization.ts`) to show or hide actions (e.g. the
  Notifications "Send" action on `notification.send`).

- **Fetch full detail on dialog open — list DTOs are incomplete.** The Identity module's user/role
  list projections never populate `Roles`/`Claims`; only get-by-id does. Edit dialogs re-fetch full
  detail on open (via a `get-*-detail-action.ts`) rather than trusting the row they were opened with,
  which would silently wipe those arrays on save. The picklist itself is fetched the same way (on
  open, not preloaded as a page prop).

- **Feature-owned people-search combobox.** `notifications`' `UserSelect` is a debounced (300ms),
  min-3-char picker backed by `identity/users`' `searchUsersAction`, owned by the feature rather than
  `components/shared/*`, because it needs a Server Action from another module and
  `components/shared/*` may not depend on a feature/module.

- **Trigger-embedded clear (X) affordance, not an external button, on every optional picker.** The
  shared `Combobox` primitive and any async feature-owned picker that accepts an `onClear` prop
  (`UserSelect` in `notifications`) render their clear control *inside* the trigger — an
  absolutely-positioned `X` button overlaid on the trigger's `relative` wrapper, replacing the chevron
  icon (hidden via `invisible`, not removed, so trigger width doesn't shift) — rather than composing a
  separate clear button beside the trigger. `onClear` is optional and omitted on required-selection
  fields. This is the convention for any new clearable picker.

- **Controlled form state alongside `useActionState`; force-remount via a bumped `key`.** Mutation
  dialogs keep their own `useState<FormValues>` in parallel — React resets *uncontrolled* fields once
  a Server Action settles, which would wipe input after a validation error. `useActionState` has no
  imperative reset, so each table bumps a per-dialog key counter on open, forcing a fresh instance
  (fresh action state, fresh form state, fresh detail fetch).

- **Generic presentational `DataTable<TData>`.** `components/shared/data-table/` composes a toolbar,
  a body (skeleton / empty / error states), and a windowed-pagination footer — fully prop-controlled,
  no dependency on any feature or data-fetching library, every prop beyond `columns`/`data`/`rowKey`
  optional. Consumers range from full server-driven search+pagination (Users) to local client-side
  filtering (Roles) to a custom multi-field filter (Notifications). Optional per-column client-side
  sort and `virtualized`/`infinite` modes are additive; no current caller uses them.

- **Context-provider-per-concern for client state.** `SidebarProvider`, `AccentColorProvider`, the
  `next-themes` wrapper, and `NotificationsProvider` each own one slice via a throwing custom hook
  (call the underlying hook once, expose via Context, throw if consumed outside the provider). The
  first three persist to `localStorage`; `NotificationsProvider` wraps live server data + one shared
  SignalR connection instead. `SearchBox` is the counter-example — a single-consumer palette keeping
  state local rather than adopting the unused `CommandPaletteProvider`.

- **Toast via a themed `sonner` wrapper.** Call sites use `notifySuccess`/`notifyError` (`notify.ts`),
  never `sonner`'s `toast` directly; the visual theme is centralized in `toast-theme.ts`. Two small
  hooks (`use-guarded-action.ts`, `use-action-success-toast.ts`) centralize the run-action-then-toast
  and toast-on-`useActionState`-success patterns each dialog would otherwise duplicate. One deliberate
  exception: `create-user-dialog.tsx`'s domain-lookup result renders inline, not as a toast.

- **Deploy-induced stale-tab errors self-recover in the error boundaries.**
  `lib/shared/deployment-recovery.ts` (`"use client"`) recognizes a rotated Server Action ID / dropped
  chunk / mid-restart fetch failure / Next `E394`, then polls `/api/health` on a growing backoff and
  hard-reloads *only once the probe answers*, capped at 5 reloads / 5 min via `sessionStorage`. Both
  `error.tsx` boundaries branch on `isRecoverableDeploymentError(error)` before the generic alert.
  Backend-connectivity failures never reach this path — `guardCall` already turns those into a
  rendered `Result`.

- **Real-time push via SignalR — browser-direct, dedicated hub token, server-resolved hub URL.**
  `use-notifications.ts` opens a `HubConnection` straight from the browser to an absolute backend URL
  (not proxied). Both the hub URL and the handshake token come from `getSignalRTokenAction()` on every
  (re)connect (via an `accessTokenFactory`, so `withAutomaticReconnect()` always re-mints) —
  `SIGNALR_HUB_URL` is a server-only env var, not `NEXT_PUBLIC_`-inlined, so changing it needs only a
  server restart. The token is **not the session JWT**: the action calls `POST auth/token/hub`
  (`signalr.api.ts`) for a purpose-built ~120s token scoped to the hub audience (`uid`+`jti` only),
  which the backend rejects on `/api`. It first refreshes a near-expiry session so the (authenticated)
  mint call succeeds. This is one of two deliberate exceptions to the httpOnly-cookie invariant — the
  other being the super-admin-gated "Session tokens" card on `/user-profile` that renders the raw
  session tokens for inspection. Failed connects retry after 30s; logging is pinned to
  `LogLevel.Critical` to silence expected 1006 closures on route-away.

- **A Popover nested inside a Dialog portals into the Dialog's own node, not `document.body`.**
  `dialog.tsx` centers via a flex wrapper (not a `transform`, which would break a nested Popover's
  fixed-position math) and hands its DOM node down through `foundation/portal-container.ts`;
  `popover.tsx` reads that context for `Popover.Portal`'s `container`, so Radix's modal scroll lock
  treats the Popover as inside the dialog. Automatic for any `Combobox`/`Popover` in a `Dialog`.

- **List-mutating Server Actions self-invalidate via `revalidatePath`.** Each create/update/delete
  action for Users, Roles, and Notifications calls `revalidatePath` for its list route right before
  returning success. Deliberately not applied to client-managed reads (notification mark-read) or
  on-demand detail/picklist fetches.

- **Multiple login entry points converge on one `establishSession()`.** Password login
  (`login-action.ts`) and the Microsoft PKCE relay (`app/login/microsoft/callback/route.ts`) both call
  the same extracted `establish-session.ts` once they have a `TokenDto`, so the resulting session is
  identical regardless of how the token was obtained — see [overview.md § Auth Flow](./overview.md#auth-flow).

## Module / Route Boundaries

Two route areas: `(dashboard)` (wraps every authenticated page with `resolveSession()` + `SessionGate`
+ `AppShell`) and the ungrouped `/login` (root layout only, no shell). One non-page route,
`app/api/health/route.ts`, is excluded from the `proxy.ts` matcher.

Every leaf under the "Administration" group is gated on that feature's own view permission via
`requirePermission()`. Home (`/`) and `/user-profile` need only a valid session.

Nav group nodes (e.g. `/administration`) have no `page.tsx` and 404 if followed; being ungated, they
still appear in the sidebar and ⌘K palette.

Feature/module isolation is enforced by convention (barrel-only cross-module imports) with a reasoned
exception set — see [dependency-graph.md](./dependency-graph.md#circular-references).

## Shared Kernel / Common Building Blocks

- `components/ui/*` — the app's own primitive layer; `combobox.tsx` (+ `popover`/`command`) is the
  shadcn-style single-select, currently used only by `identity/users`' edit dialog. `number-input.tsx`
  (`NumberInput`) is a hand-written text input (`inputMode="decimal"`, not native `type="number"`) for
  editable numeric fields — groups the integer part by thousands live as the user types and preserves
  cursor position across the reformat, while leaving the fraction part untouched so a trailing typed
  zero is never dropped; no current feature uses it.
- `components/foundation/*` — serve only `components/command/*`, the virtualized DataTable body, and
  (`portal-container.ts`) the Dialog/Popover nesting fix.
- `components/shared/data-table/*` — the generic list-table block; consumed by every list-bearing
  module with different search/sort/pagination wiring.
- The rest of `components/shared/*` and `components/toast/*` — feature-agnostic; `access-denied.tsx`
  is returned by `require-permission.tsx`, not imported by feature code.
- `lib/shared/*` and `lib/server/*` — the client-safe helpers and the server-only building blocks
  every `api/` layer sits on (see [Layering](#layering)).
- Permission-string constants and `NavItem` metadata are **not** shared kernel — each lives in its
  owning feature/module's `constants/`, assembled (not owned) by `constants/nav-items.ts`.

`clients/admin` is the only app under `clients/` — nothing is shared with a sibling app because none
exists. `pnpm-workspace.yaml` only configures build-script approval, not a multi-app workspace.

## Known Architectural Risks / Debt

| Finding | Severity | Notes |
|---|---|---|
| `proxy.ts` uses Node's `crypto` with no explicit runtime pin | Low–Medium (verify) | `token-cipher.ts` (transitive via `cookie-codec.ts`) uses `createCipheriv`/`createDecipheriv`, unsupported on the classic Edge runtime. `proxy.ts` has no `export const runtime = "nodejs"`; behaviour is consistent with the `proxy.ts` convention defaulting to Node, but that's inferred, not pinned. |
| SignalR connects browser→backend directly, bypassing Next entirely | Low | Depends on the backend's CORS allow-list including the admin origin (`StarterKit.WebApi` allows `http://localhost:3000` in its settings — see [StarterKit.WebApi § Configuration](../../../../docs/architecture/projects/WebApi.md#configuration)). A deployed origin must be added there; if missing, the handshake fails and retries silently every 30s. |
| `NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` must be constant across deploys | Low (mitigated) | Unset ⇒ a fresh key per `next build` ⇒ every deploy rotates all Server Action IDs and open tabs hit "Failed to find Server Action". Deploy scripts hoist it from the preserved `standalone/.env`; `deployment-recovery.ts` recovers when churn happens; still must be generated once per server and never changed. |
| Nav group nodes have no `page.tsx` | Low | Group nodes 404 if followed; ungated, so they surface in the sidebar and ⌘K palette. |
| Detail-route breadcrumb shows a generic label for an id segment | Low (cosmetic) | `breadcrumbs.tsx` is path-based against `NAV_ITEMS`; `isOpaqueId()` renders `"Details"` for a UUID/hex/numeric segment. No channel for a detail page to inject a real crumb label. |
| Localized timestamps use the hydration-safe `LocalDateTime` only in `notifications` | Low | Other client-rendered `toLocaleString()` sites (`session-lifecycle.tsx`, `object-viewer/utils.ts`) still use the bare form. |
| Backend list endpoints never populate `Roles`/`Claims` on the DTO | Low (worked around) | Only get-by-id populates them. Both edit dialogs re-fetch on open; any future list-reading feature would silently get empty arrays if it forgot to. |
| No `not-found.tsx` anywhere under `app/` | Low (cosmetic) | An unmatched route renders Next's default 404 in the root layout only, unmounting the whole `(dashboard)` subtree (and any open SignalR connection). |
| `prettier` + `prettier-plugin-tailwindcss` installed, no config file / `format` script | Low | Unknown whether formatting is enforced anywhere. |
| No automated test suite | Low (by design at this stage) | No test runner in `package.json`. Notable given the `SessionGate`/`ensureFreshSessionAction` state machine, the `deployment-recovery.ts` loop, and every CRUD flow are untested. |
| `components/ui/{button,tabs,dialog}.tsx` hand-modified beyond shadcn CLI output | Low | Re-running the CLI would silently drop the customizations (`button` `loading` prop + `cursor-pointer`, `tabs` `cursor-pointer`, `dialog` `max-h`/`overflow-y-auto` + portal-container). |
| `DataTable`'s `onExport` prop, `components/shared/object-viewer/*`, `CommandPaletteProvider`, `NumberInput` — all unused | Low / Trivial | Dead capability until a consumer needs it. |
| `eslint.config.mjs`'s `react-hooks/refs` override glob lists a non-existent `src/components/select/**` | Trivial | No-op glob; remove next time the file is touched. |

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-30_
