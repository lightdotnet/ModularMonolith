---
description: Load frontend/clients-only context (root CLAUDE.md + clients/<app>/CLAUDE.md, plus clients/<app>/docs/ as needed)
---

Load frontend/clients-scoped context for this session:

1. Read the root `CLAUDE.md` if not already read this session.
2. If the request names a specific client app, read that app's `clients/<app-name>/CLAUDE.md`. If it doesn't name one and more than one app exists under `clients/`, ask which app rather than guessing — see [skills/analyze-client.md](../skills/analyze-client.md) vs [skills/analyze-frontend.md](../skills/analyze-frontend.md). Today there is only one app, so this resolves to `clients/admin/CLAUDE.md`.
3. Read from that app's `docs/architecture/` and `docs/conventions/` only the specific files the task actually needs (e.g. `clients/admin/docs/architecture/overview.md`, `clients/admin/docs/conventions/coding-conventions.md`) — don't load the whole `docs/` tree up front.

Do not read `src/CLAUDE.md`, anything under `src/docs/`, or anything else under `src/` as part of this command — this is a clients-only context load. If the user's next request turns out to also need backend context, load it then via `/context-backend`, or use `/context-full` next time to load both up front.

After loading, briefly confirm what was loaded and wait for the user's actual request — do not start analyzing or summarizing the frontend unprompted.
