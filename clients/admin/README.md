# Admin Dashboard

Admin dashboard client app for the StarterKit Modular Monolith — Next.js (App Router), TypeScript, Tailwind CSS v4.

Talks to the real backend (no mock data) — the Identity and Notifications modules hosted by `StarterKit.WebApi` — through one named backend client per backend module. See [docs/architecture/overview.md](docs/architecture/overview.md) for what the app covers and [docs/conventions/development-guide.md](docs/conventions/development-guide.md#environment) for the required environment variables.

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

See [CLAUDE.md](CLAUDE.md) for project-specific rules, `docs/` for architecture and convention docs, and [docs/integration.md](../../docs/integration.md) at the repo root for the backend ↔ client integration boundary.
