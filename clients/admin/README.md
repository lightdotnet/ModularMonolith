# Admin Dashboard

Admin dashboard client app for the ModularMonolith starter kit — Next.js (App Router), TypeScript, Tailwind CSS v4.

Real backend integration against all five backend modules — `src/Identity.Api`, `src/Notifications.Api`, `src/Organization.Api`, `src/Approval.Api`, `src/LeaveManagement.Api` (each wired as its own named backend client). Covers: encrypted-cookie auth with proactive token refresh; full Users/Roles CRUD (incl. a custom-claims editor); Organization administration (companies, a department/team hierarchy, employees, employee↔login linking); a generic multi-level Approvals workflow; self-service Leave requests with an approver picker; real-time Notifications (SignalR) with a topbar bell and a Home-page inbox; permission-gated navigation. No mock data.

## Getting Started

```bash
pnpm install
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000).

## Scripts

- `pnpm dev` — start the dev server
- `pnpm build` — production build
- `pnpm start` — serve a production build
- `pnpm lint` — run ESLint

## Docs

See [CLAUDE.md](CLAUDE.md) for project-specific rules, and `docs/` for architecture and convention docs.
