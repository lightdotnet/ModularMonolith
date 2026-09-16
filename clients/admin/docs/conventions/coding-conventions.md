# Coding Conventions: admin

Conventions specific to `clients/admin`. Architectural structure and the design patterns behind
these rules live in [architecture.md](../architecture/architecture.md) — this file is the "how to
write code that matches" companion, not a second architecture doc.

## Build & Tooling

- **Next.js 16**, App Router (`src/app/`, no `pages/`). React 19, TypeScript `strict: true`,
  target `ES2017`, module resolution `bundler`, path alias `@/*` → `./src/*`.
- **Package manager**: pnpm (`pnpm-lock.yaml`). `pnpm-workspace.yaml` only configures build-script
  approval, dependency `overrides`, and minimum-release-age exclusions — not a multi-package
  workspace. Node is pinned via `.nvmrc` + `package.json` `engines` (`node >=26.8.2`).
- **Styling**: Tailwind CSS v4, CSS-first config in `src/app/globals.css` (`@import "tailwindcss"` +
  an inline `@theme inline` block). No `tailwind.config.*`.
- **`next dev` bundler**: not pinned — no `--turbopack` flag, no `next.config.ts` override.
- **Deploy**: `clients/deploy-nssm.ps1` / `deploy-pm2.ps1` / `init-nssm.ps1` (PowerShell, Windows
  host). Both deploy scripts hoist `NEXT_SERVER_ACTIONS_ENCRYPTION_KEY` from the preserved deployed
  `.env` so Server Action IDs stay stable across deploys — see [development-guide.md](./development-guide.md).

## Style

- **Linting**: ESLint flat config (`eslint.config.mjs`), `eslint-config-next`'s `core-web-vitals` +
  `typescript` rule sets. A scoped override disables `react-hooks/refs` (the React Compiler rule) for
  `src/components/{foundation,select,command}/**` — `@floating-ui/react`'s `context`/`refs` objects
  are read during render by design. The `select` glob segment matches nothing (stale — see
  [architecture.md § Known Risks](../architecture/architecture.md#known-architectural-risks--debt)).
- **Formatting**: `prettier` + `prettier-plugin-tailwindcss` are devDependencies, but no config file
  and no `format` script exist — whether formatting is enforced is unknown.
- **File naming**: kebab-case filenames, one primary export per file matching a PascalCase
  component/function name.
- **`"use client"`**: put it on files that use hooks, state, effects, or browser APIs; everything
  else stays a Server Component by default. `lib/server/*` must never carry it (build-time guarded by
  `import "server-only"`); `lib/shared/*` stays directive-free and dual-safe, with the single
  exception of `deployment-recovery.ts` (browser-only). A no-hooks presentational component omits the
  directive even when it lives beside client components (e.g. `components/ui/button-group.tsx`).
- **`"use server"`**: one file per Server Action, kebab-case, named after the action. Recurring
  return shapes:
  - mutation (`(prevState, formData) => Promise<{ error?; success? }>`), consumed via `useActionState`;
  - on-demand read (`(args) => Promise<{ data; error? }>`), called directly by a Client Component
    that can't reach the server-only `api/` layer;
  - `logoutAction` is the outlier — returns `void`, deletes the cookie and `redirect()`s itself.

  Every mutation action calls its `<name>.api.ts` function through `lib/server/backend-api.ts`
  (which injects auth), so no action extracts `session.accessToken`; each still calls
  `resolveSession()` for the "session expired" early-exit message.
- **UI primitives** (`components/ui/*`): `data-slot="<name>"` + `React.ComponentProps<...>` + `cva()`.
  Exceptions: `popover.tsx`/`command.tsx` are hand-written but follow the convention; `combobox.tsx`
  composes them and has no `data-slot` of its own; `button-group.tsx` is structural (no `cva()`).
- **UI kit visual tokens**: focus/invalid rings use `ring-2`; text-entry inputs use `rounded-md`,
  containers/buttons/popovers use `rounded-lg`; clickable Radix list rows use `cursor-pointer`.
  `button.tsx` carries hand-modifications (a `loading` prop, `cursor-pointer`) a shadcn CLI re-run
  would drop — diff after any regen.

## Structural Conventions

Full rationale for each of these is in [architecture.md § Key Design Patterns](../architecture/architecture.md#key-design-patterns);
the rule for a contributor:

- **Feature/module folder + barrel.** Add code under `src/modules/<domain>/<name>/` (or
  `src/features/home/` — the one legacy holdout; don't add new `src/features/*`). Each folder owns
  `api/`, `components/`, optionally `types/`/`constants/`/`hooks/`, and a mandatory `index.ts` barrel.
  **Cross-module imports go through the barrel** — the reasoned direct-import exceptions are listed in
  [dependency-graph.md](../architecture/dependency-graph.md#circular-references); don't add a new one
  without the same justification (avoiding a server-only export in a client bundle, or the target was
  never barrel-exported).
- **One consolidated `<name>.api.ts` per feature/module**, wrapping every backend call it makes via a
  `lib/server/backend-api.ts` instance + `lib/server/call-guard.ts`. Not one file per endpoint.
  `*-action.ts` Server Actions stay one file per action.
- **Backend client selection.** Pick the `lib/server/backend-api.ts` instance for the backend the
  endpoint belongs to — `identityApi` / `notificationsApi` / `organizationApi` / `locationApi` /
  `approvalApi` / `leaveManagementApi` / `catalogApi` / `ordersApi` (eight, one per backend module,
  all pre-wired with bearer-token auth). Don't
  import `lib/server/http.ts` directly unless you're a documented pre-session-cookie exception
  (`token.api.ts`, `user-profile.api.ts`'s `getCurrentUser`). Add a backend by adding one
  `createBackendApiClient(...)` call, not a bespoke file.
- **`call-guard.ts` helper choice**: `guardCall` / `guardResponseCall` for this backend (every
  response is envelope-wrapped); `guardRawCall` is almost never right — verify against a sibling
  endpoint.
- **Permission strings live per-feature/module** in `constants/permissions.ts`, kept in sync by hand
  with the backend's own constants (lowercase, dotted — e.g. `identity.users.view`,
  `approval.requests.view_all`). Nav metadata follows the same ownership: one `constants/nav-item.ts`
  per nav-bearing feature/module; `constants/nav-items.ts` only assembles them.
- **Page-level permission gate**: call `lib/server/require-permission.tsx`'s `requirePermission(permission)`
  at the top of the page, then `if (denied) return denied;` before the data fetch. Don't inline a
  per-page session-resolve / check / `Empty` block. `modules/leave-requests` is the deliberate
  exception (no view permission — see architecture.md).
- **`lib/server/` vs `lib/shared/`**: `lib/server/*` is server-only (env, fetch wrapper, cookie
  crypto, JWT, refresh, the page gate); `lib/shared/*` is client-safe and used by 2+ features or
  layout chrome. Permission logic lives in `lib/shared/authorization.ts` (plain args); the
  `lib/server/authorization.ts` wrapper derives `userName` from the session.
- **`app/` is routing-only** — every `page.tsx`/`layout.tsx` re-exports a feature/module component
  or composes `AppShell` + a session call; no business logic. The one non-page route,
  `app/api/health/route.ts`, is a static `204`.
- **Presentational shared components take no feature/module dependency** — `components/shared/*`,
  `components/toast/*`, `components/foundation/*`, `components/command/*`, `components/ui/*` import
  only each other, `lib/shared/utils`, and libraries.
- **Centralized toast/pending feedback**: use `hooks/use-guarded-action.ts` (imperative mutate +
  toast) and `hooks/use-action-success-toast.ts` (`useActionState` success toast) rather than
  hand-rolling `useTransition` + `notify*` per dialog.
- **Controlled form state alongside `useActionState`** for any dialog whose action can fail, and a
  bumped remount `key` to reset action/form state on open — see architecture.md.
- **Error surfacing**: mutation actions return a typed `{ error? }` to `useActionState` rather than
  throwing; list pages pass a `DataTableErrorState`-shaped `error` prop to `DataTable` (which always
  renders) instead of short-circuiting to a standalone `Alert`. `call-guard.ts` normalizes every
  thrown error into the backend's `Result`/`ApiResponse` shape; `http.ts`'s `extractErrorMessage()`
  pulls a real backend message out of a non-2xx body first.

## Testing Conventions

None installed — no Jest/Vitest/Playwright/Testing Library, no `*.test.*`/`*.spec.*`, no `test`
script. A known gap (real auth/session/CRUD logic is untested), not a convention to follow.

## Deviations From Norms Elsewhere in the Repo

`clients/admin` is the only client app, so there is no sibling to deviate from. The root `CLAUDE.md`'s
stated client defaults (Next.js App Router, TypeScript, React) all hold. Conventions established here
first — the `src/`-rooted feature/module + barrel layout, the consolidated `<name>.api.ts`,
`requirePermission`/`AccessDenied`, the named-backend-client-per-module model, the `lib/server` /
`lib/shared` split — are a candidate baseline for a future second app, not yet a cross-repo norm.

## Notes

<!-- manual: content below this line is human-authored and must be preserved verbatim during sync -->

---
_Last synced: 2026-09-16_
