---
name: context-frontend
description: Load client-app context in one step (clients/<app>/CLAUDE.md, plus clients/<app>/docs/ and docs/integration.md only as the task needs), then wait for the actual request.
---

Load client-app context for this session:

1. Root `CLAUDE.md` is already loaded — don't re-read it.
2. `Glob clients/*/` to find the app(s) — today `clients/admin/` is the only one. If more than one exists and the request doesn't name one, ask.
3. Read that app's `clients/<app-name>/CLAUDE.md`.
4. Read from its `docs/architecture/` and `docs/conventions/` only the files the task needs (e.g. `clients/admin/docs/architecture/overview.md`, `clients/admin/docs/conventions/coding-conventions.md`) — don't load the whole tree up front. Read root `docs/integration.md` only if the task touches the backend ↔ client boundary (auth flow, SignalR handshake, API contract).

Do not read backend docs under `docs/architecture/`/`docs/conventions/` or anything under `src/` as part of this skill — load backend context later only if the request turns out to need it.

After loading, briefly confirm what was loaded and wait for the user's actual request — do not start analyzing or summarizing the client unprompted.
