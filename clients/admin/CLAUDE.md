# Admin Client

Project-specific guidance for `clients/admin/`. See the root [CLAUDE.md](../../CLAUDE.md) for repository-wide rules (language convention, code-change workflow gate, agent/skill/workflow usage) — this file only covers what's specific to this app. See also [README.md](README.md) for human-facing getting-started instructions (install/dev/build scripts).

## Purpose

Internal admin console for the ModularMonolith starter template — the first and, so far, only client app. Real backend integration against `Identity.Api`, `Notifications.Api`, `Organization.Api`, `Approval.Api`, and `LeaveManagement.Api` (each wired as its own named backend client): encrypted-cookie auth (password, or Microsoft via a PKCE authorization-code relay — see [docs/architecture/overview.md § Auth Flow](docs/architecture/overview.md#auth-flow)) with proactive token refresh, full Users/Roles CRUD (incl. a custom-claims editor), real-time Notifications (SignalR) with a topbar bell and a Home-page inbox, Organization administration (Companies, Departments & Teams, Employees — including assigning employees into the department/team hierarchy with a status/manager flag and creating/linking their Identity login), a generic Approvals workflow (per-user pending decisions plus an admin test harness for building multi-level approver chains), self-service Leave requests (every authenticated user submits/edits/deletes their own requests with a required approver picker resolved from real department candidates, plus a `leave.requests.manage`-gated view-all/delete-any surface), permission-gated navigation.

## Stack

Next.js 16 (App Router, rooted at `src/app/`), React 19, TypeScript (`strict: true`), Tailwind CSS v4 (CSS-first config), shadcn-generated primitives on `radix-ui` + `class-variance-authority`, `next-themes`, `@microsoft/signalr`, pnpm.

## Architectural Constraints ("do not" rules)

- **One consolidated `<feature>.api.ts` file per feature** under `modules/<domain>/<feature>/api/` (or `features/home/` for the one feature that hasn't moved under `modules/`) — not one file per endpoint; `*-action.ts` Server Actions stay one file per action.
- **Cross-feature imports go through a feature's `index.ts` barrel** — only narrow, reasoned exceptions (server-only-export avoidance) bypass it directly; see [docs/architecture/architecture.md § Key Design Patterns](docs/architecture/architecture.md#key-design-patterns) for the current exception list.
- **`lib/server/*` is server-only** (never import from a Client Component) — `lib/shared/*` is the client-safe split.
- Auth/session state lives only in the encrypted `admin_session` cookie — never pass an access token through any other channel except two deliberate exceptions: the SignalR-handshake token (see Architecture below) and the short-lived PKCE `code_verifier` cookie used only to complete the Microsoft login relay.

## Architecture

- [docs/architecture/overview.md](docs/architecture/overview.md) — purpose, structure, key routes/areas, backend integration, auth flow.
- [docs/architecture/architecture.md](docs/architecture/architecture.md) — layering, dependency direction, key design patterns, known risks/debt.
- [docs/architecture/dependency-graph.md](docs/architecture/dependency-graph.md) — external package references, circular-import check.

## Conventions

- [docs/conventions/coding-conventions.md](docs/conventions/coding-conventions.md) — build/tooling, style, structural conventions.
- [docs/conventions/development-guide.md](docs/conventions/development-guide.md) — local setup, common tasks, where to look for X.

## Testing

No automated test suite exists yet (no Jest/Vitest/Playwright/Testing Library) — a known gap, not a convention to follow. See [docs/architecture/architecture.md § Known Architectural Risks / Debt](docs/architecture/architecture.md#known-architectural-risks--debt).

## Note for a future second client app

Nothing in this repo currently documents conventions shared *across* client apps — `admin` is the only one, and everything above is specific to it. If a second app under `clients/` is added, revisit whether any of this generalizes into a genuinely cross-client-apps doc rather than assuming `admin`'s conventions apply by default.

---
_Last synced: 2026-09-11_

<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->
