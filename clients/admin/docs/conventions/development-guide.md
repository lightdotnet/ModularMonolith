# Development Guide: admin

How to set up, run, and make common changes to `clients/admin`. Architectural background is in
[../architecture/overview.md](../architecture/overview.md) and
[../architecture/architecture.md](../architecture/architecture.md) — this guide stays task-oriented.

## Prerequisites

- **Node.js**: `.nvmrc` pins `26.8.2`; `package.json` `engines` requires `node >=26.8.2`.
  `@types/node` is `^26.5.1`.
- **pnpm**: required (`pnpm-lock.yaml` is the only lockfile). Version not pinned (no `packageManager`).
- **A reachable backend**: login, profile, notifications, and every feature page make real HTTP calls.
  All eight `*_API_BASE_URL` vars must point at a running instance (currently one co-hosted backend).
  SignalR notifications additionally need the backend reachable **directly from the browser**
  (`SIGNALR_HUB_URL` resolvable + backend CORS allowing the admin origin).

## Scripts

```bash
pnpm dev     # next dev — default port 3000
pnpm build   # next build — output: "standalone" (per next.config.ts)
pnpm start   # next start — serves a prior pnpm build
pnpm lint    # eslint (eslint.config.mjs)
```

Local dev talks to the backend over plain HTTP (`http://localhost:5000`): the backend disables
HTTPS redirection in Development, so the server-side `fetch` calls in `lib/server/*` reach it
directly without tripping over the untrusted self-signed ASP.NET dev certificate. The backend's
`https` launch profile still exposes `https://localhost:5001` for anyone who needs it. Set the
`*_API_BASE_URL` / `SIGNALR_HUB_URL` vars to match (see `.env.example`). The `next dev` bundler is
not pinned (no `--turbopack` flag, no config override).

## Environment

`.gitignore` ignores `.env*` except the committed `.env.example`, which is the template. All vars are
server-only (never `NEXT_PUBLIC_`):

| Var | Purpose |
|---|---|
| `IDENTITY_API_BASE_URL`, `NOTIFICATIONS_API_BASE_URL`, `ORGANIZATION_API_BASE_URL`, `LOCATION_API_BASE_URL`, `APPROVAL_API_BASE_URL`, `LEAVE_MANAGEMENT_API_BASE_URL`, `CATALOG_API_BASE_URL`, `ORDERS_API_BASE_URL` | Base URL per backend module. Must include the full path prefix (e.g. `api/v1/`) and a trailing slash — `lib/server/http.ts` prepends nothing. Configured independently even though they currently share one host. |
| `TOKEN_ENCRYPTION_KEY` | 32-byte base64 key (`openssl rand -base64 32`). AES-256-GCM key for the `admin_session` cookie. `lib/server/config.ts` throws if unset; read on every request. |
| `NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` | Base64 AES key Next.js uses at **build time** to salt Server Action IDs. Not read by app code. If unset, a fresh key per build ⇒ every deploy breaks open tabs with "Failed to find Server Action". Optional locally; must be set and **constant forever** on deployed servers. |
| `SIGNALR_HUB_URL` | Absolute URL to the backend SignalR hub. Read server-side and handed to the browser at connect time by a Server Action (not inlined), so it's a runtime setting — change it by editing the server `.env` and restarting, no rebuild. |

## Deploying

Three PowerShell scripts at `clients/` (not `clients/admin/`) deploy the standalone build to a
Windows host:

- `clients/deploy-nssm.ps1` — IIS + NSSM Windows Service. `pnpm install --frozen-lockfile` →
  `pnpm build` → copy `.next/standalone` (+ `.next/static`, `public/`) into the live folder,
  preserving deployed `.env*` → cycle the app pool, site, and service. Wipes only `standalone/`.
- `clients/deploy-pm2.ps1` — the PM2 variant of the same flow.
- `clients/init-nssm.ps1` — one-time NSSM service registration.

Both deploy scripts run `Get-ServerActionsEncryptionKey` first, hoisting
`NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` from the preserved `standalone/.env` into the build environment
before `pnpm build`. Generate it once per server, put it in `standalone/.env`, never change it.

## Testing

None — no test runner installed, no `*.test.*`/`*.spec.*` files.

## Common Tasks

| Task | How |
|---|---|
| Run the dev server | `cd clients/admin && pnpm install && pnpm dev` (copy `.env.example` → `.env.local` and fill the base URLs + `TOKEN_ENCRYPTION_KEY` first) |
| Deploy | Run `clients/deploy-nssm.ps1` (or `deploy-pm2.ps1`) from a prepared Windows host; ensure `NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` is in the deployed `standalone/.env` first |
| Add a backend endpoint call | Add a function to the feature/module's `<name>.api.ts` (create it if missing), wrapping `requestJson`/`requestVoid` from the right `lib/server/backend-api.ts` instance via a `call-guard.ts` helper; export it from `index.ts` |
| Add a feature/module | Create `src/modules/<domain>/<name>/` with `api/`, optional `components/`/`types/`/`constants/`, and an `index.ts` barrel; add a re-export `page.tsx` under `src/app/`. Don't add new `src/features/*` |
| Gate a page on a permission | `requirePermission(permission)` at the top, then `if (denied) return denied;` before the data fetch — see `users-page.tsx` |
| Add a list/table page | Reuse `components/shared/data-table` (`DataTable<TData>`). Reference wirings: `users` (server-driven URL-param search), `roles` (client-side filter, no backend search), `notifications` (`customSearch` multi-field filter), the `approvals` tabs / `leave-requests` (pre-fetched array, minimal pagination) |
| Show a toast | `notifySuccess`/`notifyError` from `@/components/toast` — never import `sonner` directly |
| Imperative action + toast/pending | `useGuardedAction()` (`hooks/use-guarded-action.ts`) — see `delete-user-dialog.tsx` |
| Form dialog on a mutation action | `useActionState` + `useActionSuccessToast(state, msg, onSuccess?)` (`hooks/use-action-success-toast.ts`) — see any create/edit dialog |
| Add a shadcn primitive | `npx shadcn@latest add <component>` from `clients/admin/` (`components.json`: style `radix-nova`, base `neutral`, icons `lucide`). `button.tsx` has manual edits — diff after any regen |
| Add a nav item | Add/update the feature's `constants/nav-item.ts`, re-export from its `index.ts`, then reference it in `src/constants/nav-items.ts` **by direct file path** (not the barrel — it carries server-only code the client-side `Sidebar` must not pull in). Adding a nav entry does not create the route |

## Where to Look for X

Routing and per-feature responsibilities are in [../architecture/overview.md § Key Routes/Areas](../architecture/overview.md#key-routesareas)
and the auth flow in [§ Auth Flow](../architecture/overview.md#auth-flow). Beyond those:

| Concern | Location |
|---|---|
| App shell / global providers | `src/app/layout.tsx`, `src/app/(dashboard)/layout.tsx` → `components/layout/app-shell.tsx` |
| Login / logout | `modules/identity/auth/api/{login,logout}-action.ts` (`logout-action.ts` is imported directly by `user-menu.tsx`, not barrel-exported) |
| Session cookie: crypto, chunking, refresh | `lib/server/{session-cookie,token-cipher,stored-session,cookie-codec,session,jwt,build-session-claims,refresh-session,refetch-profile,persist-session-cookie}.ts` |
| Session freshness gate | `components/layout/session-gate.tsx` → `modules/identity/auth/api/ensure-fresh-session-action.ts`. `src/proxy.ts` is only the 7-day cap + `/login` redirect |
| Deploy-stale-tab recovery | `lib/shared/deployment-recovery.ts`, `components/layout/deployment-recovery-notice.tsx`, both `error.tsx` boundaries, `app/api/health/route.ts` |
| Server-only API plumbing | `lib/server/{http,backend-api,api-clients,call-guard,config}.ts`, `lib/server/http-handlers/bearer-token-handler.ts`. `http.ts` emits `[api]` request traces to the console **in development only** (never bodies/query/headers — those carry tokens) |
| Permission checks | `lib/shared/authorization.ts` (logic), `lib/server/authorization.ts` (wrapper), `lib/server/require-permission.tsx` (page gate), `components/shared/access-denied.tsx` |
| Real-time notifications | `modules/notifications/hooks/use-notifications.ts`, `context/notifications-provider.tsx`, `components/{notification-bell,notification-inbox}.tsx`, `api/{get-signalr-token-action,signalr.api}.ts` |
| Reusable list/table block | `components/shared/data-table/` |
| Command palette (⌘K) | `components/command/*`, wired via `components/shared/search-box.tsx` |
| Nav structure | each feature/module's `constants/nav-item.ts` + `src/constants/nav-items.ts` (assembly), `lib/shared/menu.ts` |
| Theming | `components/theme/*`, tokens in `src/app/globals.css` |
| Backend/client shared shapes | `types/api.ts` (envelope), per-feature `types/*.ts` (DTOs, barrel-re-exported) |

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-16_
