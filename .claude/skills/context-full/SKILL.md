---
description: Load full context, backend and clients (root CLAUDE.md + src/CLAUDE.md + clients/<app>/CLAUDE.md + docs/integration.md)
---

Load full-stack context for this session (both backend and clients):

1. Read the root `CLAUDE.md` if not already read this session.
2. Read `src/CLAUDE.md` and each relevant client app's `CLAUDE.md` (today, just `clients/admin/CLAUDE.md`).
3. Read `docs/integration.md` (the cross-cutting integration contract).
4. Read from `src/docs/` and `clients/<app-name>/docs/` only the specific files the task actually needs — don't load either project's whole `docs/` tree up front.

Use this instead of `/context-backend`/`/context-frontend` when the task genuinely spans both stacks (e.g. a full-stack feature, an API contract change, integration/auth wiring between a client and the backend) — see [skills/create-feature.md](../skills/create-feature.md) and [AI_CONTEXT.md § Full-Stack Safety Rules](../AI_CONTEXT.md#full-stack-safety-rules).

After loading, briefly confirm what was loaded and wait for the user's actual request — do not start analyzing or summarizing the repository unprompted.
