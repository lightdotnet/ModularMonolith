---
description: Load backend-only context (root CLAUDE.md + src/CLAUDE.md, plus src/docs/ as needed)
---

Load backend-scoped context for this session:

1. Read the root `CLAUDE.md` if not already read this session.
2. Read `src/CLAUDE.md`.
3. Read from `src/docs/architecture/` and `src/docs/conventions/` only the specific files the task actually needs (e.g. `src/docs/architecture/architecture.md`, `src/docs/architecture/modules/<Name>.md`, `src/docs/conventions/coding-conventions.md`) — don't load the whole `src/docs/` tree up front.

Do not read `clients/admin/CLAUDE.md`, anything under `clients/admin/docs/`, or anything else under `clients/` as part of this command — this is a backend-only context load. If the user's next request turns out to also need client context, load it then via `/context-frontend`, or use `/context-full` next time to load both up front.

After loading, briefly confirm what was loaded and wait for the user's actual request — do not start analyzing or summarizing the backend unprompted.
